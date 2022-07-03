using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.TestAdapter.Utils;

namespace Tests.Sailfish.TestAdapter.Utils;

public static class DllFinder
{
    public static List<string> FindAllDllsRecursively()
    {
        var projFileRecursor = new DirectoryRecursion();
        var projFile = projFileRecursor.RecurseUpwardsUntilFileIsFound(".csproj", Directory.GetFiles(".").First(), 5);

        var fileFinder = new DirectoryRecursion();
        var allDlls = fileFinder.FindAllFilesRecursively(projFile, "*.dll", path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
        return allDlls;
    }
}