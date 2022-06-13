using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter.Utils;

internal class FileIo
{
    public List<string> FindAllFilesRecursively(FileInfo originReferenceFile, string searchPattern, Func<string, bool>? where = null)
    {
        var filePaths = Directory.GetFiles(
            Path.GetDirectoryName(originReferenceFile.FullName)!,
            searchPattern,
            SearchOption.AllDirectories);

        if (where is not null)
        {
            filePaths = filePaths.Where(FilePathDoesNotContainBinOrObjDirs).ToArray();
        }
        
        foreach (var filePath in filePaths)
        {
            logger.Verbose($"Corresponding {searchPattern} files in this assembly project");
            logger.Verbose("--- {filePath}", filePath);
        }

        return filePaths.ToList();
    }

    public bool FilePathDoesNotContainBinOrObjDirs(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return !(path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}"));
    }

    // public string[]? ReadAndSplitFileContents(string sourceFile) // hmmm do I use this?
    // {
    //     var content = ReadFile(sourceFile);
    //     return content.Split("\r");
    // }

    public string ReadFileContents(string sourceFile)
    {
        var content = ReadFile(sourceFile);
        return content;
    }

    private string ReadFile(string filePath)
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