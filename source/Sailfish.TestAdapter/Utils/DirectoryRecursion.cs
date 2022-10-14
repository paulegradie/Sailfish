using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Utils;

internal class DirectoryRecursion
{
    public FileInfo RecurseUpwardsUntilFileIsFound(string suffixToMatch, string sourceFile, int maxParentDirLevel)
    {
        var result = RecurseUpwardsUntilFileIsFoundInner(suffixToMatch, sourceFile, maxParentDirLevel);
        if (result is null) throw new Exception("Couldn't locate a csproj file in this project.");
        logger.Verbose("Project found: {0}", result.FullName);
        return result;
    }

    private FileInfo? RecurseUpwardsUntilFileIsFoundInner(string suffixToMatch, string sourceFile, int maxParentDirLevel)
    {
        if (maxParentDirLevel == 0) return null; // try and stave off disaster

        // get the directory of the source
        var dirName = Path.GetDirectoryName(sourceFile);
        if (dirName is null) return null;

        var csprojFile = Directory.GetFiles(dirName).Where(x => x.EndsWith(suffixToMatch)).SingleOrDefault();
        if (csprojFile is null)
        {
            if (ThereIsAParentDirectory(new DirectoryInfo(sourceFile), out var parentDir))
                return RecurseUpwardsUntilFileIsFoundInner(suffixToMatch, parentDir.FullName, maxParentDirLevel - 1);
            return null;
        }

        logger.Verbose("Found the proj file! {0}", csprojFile);
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

    public List<string> FindAllFilesRecursively(FileInfo originReferenceFile, string searchPattern, Func<string, bool>? where = null)
    {
        var filePaths = Directory.GetFiles(
            Path.GetDirectoryName(originReferenceFile.FullName)!,
            searchPattern,
            SearchOption.AllDirectories);

        if (where is not null) filePaths = filePaths.Where(where).ToArray();

        foreach (var filePath in filePaths)
        {
            logger.Verbose($"Corresponding {searchPattern} files in this assembly project");
            logger.Verbose("--- {filePath}", filePath);
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