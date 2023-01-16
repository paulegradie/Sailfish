using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Sailfish.TestAdapter.Utils;

internal static class TestDiscovery
{
    private static readonly DirectoryRecursion DirRecursor = new();
    private static readonly FileIo FileIo = new();
    private static readonly TestCaseItemCreator TestCaseCreator = new();

    /// <summary>
    /// This method should scan through the dlls provided, and extract each type that qualifies as a test type
    /// Then, that 
    /// </summary>
    /// <param name="sourceDllPaths"></param>
    /// <returns></returns>
    public static IEnumerable<TestCase> DiscoverTests(IEnumerable<string> sourceDllPaths)
    {
        var testCases = new List<TestCase>();
        foreach (var sourceDllPath in sourceDllPaths.Distinct())
        {
            var project = DirRecursor.RecurseUpwardsUntilFileIsFound(
                ".csproj",
                sourceDllPath,
                10);

            Type[] perfTestTypes;
            try
            {
                perfTestTypes = TypeLoader.LoadSailfishTestTypesFrom(sourceDllPath);
            }
            catch
            {
                continue;
            }

            if (perfTestTypes.Length == 0) continue;

            var correspondingCsFiles = DirRecursor.FindAllFilesRecursively(
                project,
                "*.cs",
                DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs);
            if (correspondingCsFiles.Count == 0) continue;

            foreach (var csFilePath in correspondingCsFiles)
            {
                var fileContent = FileIo.ReadFileContents(csFilePath);
                var perfTypesInThisCsFile = TestFilter.FindTestTypesInTheCurrentFile(fileContent, perfTestTypes);

                foreach (var perfTestType in perfTypesInThisCsFile)
                {
                    var cases = TestCaseCreator.AssembleTestCases(perfTestType, fileContent, csFilePath, sourceDllPath);
                    testCases.AddRange(cases);
                }
            }
        }

        return testCases;
    }
}