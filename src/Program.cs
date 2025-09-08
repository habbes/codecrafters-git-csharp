using System;
using System.IO;
using System.IO.Compression;

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
else if (command == "cat-file -p" && args.Length == 3 && args[0] == "-p")
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
else
{
    throw new ArgumentException($"Unknown command {command}");
}