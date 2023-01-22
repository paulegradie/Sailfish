using System;
using System.IO;

namespace Sailfish.TestAdapter.Discovery;

internal static class FileIo
{
    public static string ReadFileContents(string sourceFile)
    {
        var content = ReadFile(sourceFile);
        return content;
    }

    private static string ReadFile(string filePath)
    {
        try
        {
            using var fileStream = new StreamReader(filePath);
            var content = fileStream.ReadToEnd();
            return content;
        }
        catch
        {
            throw new Exception($"Could not read the file provided: {filePath}");
        }
    }
}