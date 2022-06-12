using System.IO;
using System.Linq;
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

        var hardCodedDir = "C:\\Users\\paule\\code\\VeerPerformaRelated\\TestingLogs";

        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .WriteTo.File(Path.Combine(hardCodedDir, fileName))
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