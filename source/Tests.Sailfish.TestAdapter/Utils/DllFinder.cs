using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;

namespace Tests.Sailfish.TestAdapter.Utils;

public static class DllFinder
{
    public static IEnumerable<string> FindAllDllsRecursively()
    {
        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", Directory.GetFiles(".").First(), 5, Substitute.For<IMessageLogger>());

        var allDlls = DirectoryRecursion.FindAllFilesRecursively(projFile, "*.dll", Substitute.For<IMessageLogger>(),
            path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
        return allDlls;
    }
}