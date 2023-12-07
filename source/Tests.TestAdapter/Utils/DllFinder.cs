using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;
using System.IO;
using System.Linq;

namespace Tests.TestAdapter.Utils;

public static class DllFinder
{
    public static string FindThisProjectsDllRecursively()
    {
        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", Directory.GetFiles(".").First(), 5);
        var dllName = projFile.Name.Replace("csproj", "dll");
        var allDlls = DirectoryRecursion.FindAllFilesRecursively(projFile, dllName, Substitute.For<IMessageLogger>(),
            path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

#if DEBUG
        return
            allDlls.Single(x =>
                x.Contains(Path.Join("bin", "Debug",
                    "net8.0"))); // if this throws - check that you've removed non-target directories in your bin directory or that you've changed the target framework
#else
        return allDlls.Single(x => x.Contains(Path.Join("bin", "net8.0", "Release")));
#endif
    }
}