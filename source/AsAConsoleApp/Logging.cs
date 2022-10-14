using System.IO;
using System.Linq;
using Serilog;
using Serilog.Core;

namespace AsAConsoleApp;

public static class Logging
{
    public static Logger CreateLogger(string fileName)
    {
        var currentDirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
        const string logfileDirName = "ConsoleAppLogs";

        var projectRoot = FindProjectRootDir(currentDirInfo, 5);

        var logDirPath = projectRoot is null
            ? Path.Combine(".", logfileDirName)
            : Path.Combine(projectRoot.FullName, logfileDirName);

        return new LoggerConfiguration()
            .WriteTo.File(Path.Combine(logDirPath, fileName))
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();
    }

    public static DirectoryInfo? FindProjectRootDir(DirectoryInfo? currentDirectory, int maxParentDirLevel)
    {
        if (maxParentDirLevel == 0) return null; // try and stave off disaster
        if (currentDirectory is null) return null;

        var csprojFile = currentDirectory.GetFiles("*.csproj").SingleOrDefault();
        if (csprojFile is not null) return csprojFile.Directory;

        if (currentDirectory.ThereIsAParentDirectory(out var parentDir))
        {
            return FindProjectRootDir(parentDir, maxParentDirLevel - 1);
        }
        
        return null;

    }
}

public static class ExtensionMethods
{
    public static bool ThereIsAParentDirectory(this DirectoryInfo dir, out DirectoryInfo parentDir)
    {
        if (dir.Parent is not null)
        {
            parentDir = dir.Parent;
            return dir.Parent.Exists;
        }

        parentDir = dir;
        return false;
    }
}