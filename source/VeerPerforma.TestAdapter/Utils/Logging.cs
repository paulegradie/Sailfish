using Serilog;
using Serilog.Core;

namespace VeerPerforma.TestAdapter.Utils;

public static class Logging
{
    public static Logger CreateLogger(string fileName)
    {
        var currentDirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
        var logfileDirName = "ADAPTER_LOGS";
        var projectRoot = FindProjectRootDir(currentDirInfo, 5);

        var logDirPath = projectRoot is null
            ? Path.Combine(".", logfileDirName)
            : Path.Combine(projectRoot.FullName, logfileDirName);

        return new LoggerConfiguration()
            .WriteTo.File(Path.Combine(logDirPath, fileName))
            .CreateLogger();
    }

    public static DirectoryInfo? FindProjectRootDir(DirectoryInfo? currentDirectory, int maxParentDirLevel)
    {
        if (maxParentDirLevel == 0) return null; // try and stave off disaster

        if (currentDirectory is null) return null;

        var csprojFile = currentDirectory.GetFiles("*.csproj").SingleOrDefault();
        if (csprojFile is null)
        {
            if (currentDirectory.ThereIsAParentDirectory(out var parentDir))
            {
                return FindProjectRootDir(parentDir, maxParentDirLevel - 1);
            }
            else
            {
                return null;
            }
        }

        return csprojFile.Directory;
    }
}