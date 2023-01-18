using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Sailfish.TestAdapter.Utils;

internal class DirectoryRecursion
{
    public FileInfo RecurseUpwardsUntilFileIsFound(string suffixToMatch, string sourceFile, int maxParentDirLevel, IMessageLogger logger)
    {
        var result = RecurseUpwardsUntilFileIsFoundInner(suffixToMatch, sourceFile, maxParentDirLevel, logger);
        if (result is null) throw new Exception("Couldn't locate a csproj file in this project.");
        logger.SendMessage(TestMessageLevel.Informational, $"Project found: {result.FullName}");
        return result;
    }

    private FileInfo? RecurseUpwardsUntilFileIsFoundInner(string suffixToMatch, string sourceFile, int maxParentDirLevel, IMessageLogger logger)
    {
        if (maxParentDirLevel == 0) return null; // try and stave off disaster

        // get the directory of the source
        var dirName = Path.GetDirectoryName(sourceFile);
        if (dirName is null) return null;

        var csprojFile = Directory.GetFiles(dirName).SingleOrDefault(x => x.EndsWith(suffixToMatch));
        if (csprojFile is null)
        {
            return ThereIsAParentDirectory(new DirectoryInfo(sourceFile), out var parentDir)
                ? RecurseUpwardsUntilFileIsFoundInner(suffixToMatch, parentDir.FullName, maxParentDirLevel - 1, logger)
                : null;
        }

        logger.SendMessage(TestMessageLevel.Informational, $"Found the proj file! {csprojFile}");
        return new FileInfo(csprojFile);
    }

    private bool ThereIsAParentDirectory(DirectoryInfo dir, out DirectoryInfo parentDir)
    {
        if (dir.Parent is not null)
        {
            parentDir = dir.Parent;
            return dir.Parent.Exists;
        }

        parentDir = dir;
        return false;
    }

    public static List<string> FindAllFilesRecursively(FileInfo originReferenceFile, string searchPattern, IMessageLogger logger, Func<string, bool>? where = null)
    {
        var filePaths = Directory.GetFiles(
            Path.GetDirectoryName(originReferenceFile.FullName)!,
            searchPattern,
            SearchOption.AllDirectories);

        if (where is not null) filePaths = filePaths.Where(where).ToArray();

        foreach (var filePath in filePaths)
        {
            logger.SendMessage(TestMessageLevel.Informational, $"Corresponding {searchPattern} files in this assembly project");
            logger.SendMessage(TestMessageLevel.Informational, $"--- {filePath}");
        }

        return filePaths.ToList();
    }


    public static class FileSearchFilters
    {
        public static bool FilePathDoesNotContainBinOrObjDirs(string path)
        {
            var sep = Path.DirectorySeparatorChar;
            return !(path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}"));
        }
    }
}