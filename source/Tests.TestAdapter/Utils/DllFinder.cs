using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;

namespace Tests.TestAdapter.Utils;

public static class DllFinder
{
    public static string FindThisProjectsDllRecursively()
    {
        // Prefer the executing test assembly's location; this is the DLL we want to feed into discovery
        var executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(executingAssemblyPath) && File.Exists(executingAssemblyPath))
        {
            return executingAssemblyPath;
        }

        // Fallback: locate the project and search for the built DLL (supports multiple TFMs)
        var start = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", start, 10);
        var dllName = projFile.Name.Replace(".csproj", ".dll");
        var allDlls = DirectoryRecursion.FindAllFilesRecursively(
            projFile,
            dllName,
            Substitute.For<IMessageLogger>(),
            path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        // Try to match current TFM from base directory first (e.g., net9.0, net8.0; Release or Debug)
        var baseDir = (AppContext.BaseDirectory ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar);
        var preferred = allDlls.FirstOrDefault(p => baseDir.Length > 0 && p.StartsWith(baseDir));
        if (preferred is not null) return preferred;

        // Otherwise, attempt common TFMs in priority order under Release then Debug
        var tfms = new[] { "net9.0", "net8.0" };
        foreach (var tfm in tfms)
        {
            var rel = allDlls.FirstOrDefault(x => x.Contains(Path.Join("bin", "Release", tfm)));
            if (rel is not null) return rel;
            var dbg = allDlls.FirstOrDefault(x => x.Contains(Path.Join("bin", "Debug", tfm)));
            if (dbg is not null) return dbg;
        }

        // Last resort: return the first discovered DLL
        return allDlls.First();
    }
}