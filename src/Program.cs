using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command.");
    return;
}

string command = args[0];

if (command == "init")
{
    Directory.CreateDirectory(".git");
    Directory.CreateDirectory(".git/objects");
    Directory.CreateDirectory(".git/refs");
    File.WriteAllText(".git/HEAD", "ref: refs/heads/main\n");
    Console.WriteLine("Initialized git directory");
}
else if (command == "cat-file" && args.Length == 3 && args[1] == "-p")
{
    // See: https://git-scm.com/book/en/v2/Git-Internals-Git-Objects
    string objectHash = args[2];
    string dirName = objectHash[..2];
    string fileName = objectHash[2..];
    string objectPath = Path.Combine(".git", "objects", dirName, fileName);
    using var fileStream = File.OpenRead(objectPath);
    using var zlibStream = new ZLibStream(fileStream, CompressionMode.Decompress);

    using var reader = new StreamReader(zlibStream);
    string content = reader.ReadToEnd();

    // find null byte, which separates header from content
    // header contains object type and size followed by null byte
    int nullByteIndex = content.IndexOf('\0');
    string header = content[..nullByteIndex];

    string[] headerContent = header.Split(' ');
    string type = headerContent[0];
    if (type != "blob")
    {
        throw new ArgumentException($"Unsupported object type {type}");
    }

    string body = content[(nullByteIndex + 1)..];

    Console.Write(body);
}
else if (command == "hash-object" && args.Length > 1)
{
    // See: https://git-scm.com/docs/git-hash-object
    // See: https://git-scm.com/book/en/v2/Git-Internals-Git-Objects
    bool writeObject = args.Length > 2 && args[1] == "-w";
    string filePath = args[^1];

    var fileSize = new FileInfo(filePath).Length;

    // Assums text 
    var fileContent = File.ReadAllBytes(filePath);

    string header = $"blob {fileContent.Length}\0";
    byte[] headerBytes = Encoding.UTF8.GetBytes(header);

    byte[] contentBytes = [..headerBytes, ..fileContent];

    byte[] hashBytes = SHA1.HashData(contentBytes);
    string hashString = Convert.ToHexStringLower(hashBytes);

    if (writeObject)
    {
        string dirName = hashString[..2];
        string fileName = hashString[2..];
        string objectDirPath = Path.Combine(".git", "objects", dirName);
        Directory.CreateDirectory(objectDirPath);

        var destinationPath = Path.Combine(objectDirPath, fileName);
        using var fileStream = File.OpenWrite(destinationPath);
        using var zlibStream = new ZLibStream(fileStream, CompressionMode.Compress);
        zlibStream.Write(contentBytes);
    }

    Console.WriteLine(hashString);
}
else
{
    throw new ArgumentException($"Unknown command {command}");
}