using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;

namespace Tests.Sailfish.TestAdapter.Utils;

public static class DllFinder
{
    public static string FindThisProjectsDllRecursively()
    {
        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", Directory.GetFiles(".").First(), 5, Substitute.For<IMessageLogger>());
        var dllName = projFile.Name.Replace("csproj", "dll");
        var allDlls = DirectoryRecursion.FindAllFilesRecursively(projFile, dllName, Substitute.For<IMessageLogger>(),
            path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

#if DEBUG
        return allDlls.Single(x => x.Contains(Path.Join("bin", "Debug")));
#else
        return allDlls.Single(x => x.Contains(Path.Join("bin", "Release")));
#endif
    }
}