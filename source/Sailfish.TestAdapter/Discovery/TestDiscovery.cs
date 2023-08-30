using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Discovery;

internal static class TestDiscovery
{
    /// <summary>
    /// This method should scan through the dlls provided, and extract each type that qualifies as a test type
    /// </summary>
    /// <param name="sourceDllPaths"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static IEnumerable<TestCase> DiscoverTests(IEnumerable<string> sourceDllPaths, IMessageLogger logger)
    {
        var hasher = new HashWrapper();
        var testCases = new List<TestCase>();
        FileInfo? previousSearchDir = null;
        FileInfo? project = null;
        foreach (var sourceDllPath in sourceDllPaths.Distinct())
        {
            var currentSearchDir = new FileInfo(sourceDllPath);
            if (previousSearchDir is null || (currentSearchDir.Directory is not null && previousSearchDir.Directory is not null &&
                                              currentSearchDir.Directory?.FullName != previousSearchDir.Directory?.FullName))
            {
                project = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(
                    ".csproj",
                    sourceDllPath,
                    10);
                previousSearchDir = currentSearchDir;
            }

            if (project is null)
            {
                const string msg = "Failed to discover the test project";
                logger.SendMessage(TestMessageLevel.Error, msg);
                throw new Exception(msg);
            }

            Type[] perfTestTypes;
            try
            {
                perfTestTypes = TypeLoader.LoadSailfishTestTypesFrom(sourceDllPath, logger);
            }
            catch
            {
                continue;
            }

            if (perfTestTypes.Length == 0) continue;

            var projectSourceCodeFilePaths = DirectoryRecursion.FindAllFilesRecursively(
                project,
                "*.cs",
                logger,
                DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs);
            if (projectSourceCodeFilePaths.Count == 0) continue;

            var classMetaDatas = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                    projectSourceCodeFilePaths,
                    perfTestTypes,
                    nameof(SailfishAttribute).Replace(nameof(Attribute), ""),
                    nameof(SailfishMethodAttribute).Replace(nameof(Attribute), ""))
                .OrderBy(meta => meta.ClassName);

            foreach (var classMetaData in classMetaDatas)
            {
                var classTestCases = TestCaseItemCreator
                    .AssembleTestCases(classMetaData, sourceDllPath, hasher)
                    .ToList();

                if (classTestCases.Count == 0) continue;
                var numVariables = classTestCases.First().Variables.Length;

                var orderedCases = SortTestCases(numVariables, classTestCases);
                testCases.AddRange(orderedCases);
            }
        }

        return testCases;
    }

    private static List<TestCase> SortTestCases(int numVariables, List<OrderableTestCases> classTestCases)
    {
        var orderedCases = new List<TestCase>();
        classTestCases = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty)).ToList();
        try
        {
            switch (numVariables)
            {
                case 1:
                    var c1 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty))
                        .ThenBy(x => x.Variables[0] as int? ?? x.Variables[0]).Select(x => x.TestCase);
                    orderedCases.AddRange(c1);
                    break;
                case 2:
                    var c2 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty)).ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1]).Select(x => x.TestCase);
                    orderedCases.AddRange(c2);
                    break;
                case 3:
                    var c3 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty)).ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1]).ThenBy(x => x.Variables[2]).Select(x => x.TestCase);
                    orderedCases.AddRange(c3);
                    break;
                case 4:
                    var c4 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty)).ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1]).ThenBy(x => x.Variables[2]).ThenBy(x => x.Variables[3])
                        .Select(x => x.TestCase);
                    orderedCases.AddRange(c4);
                    break;
                case 5:
                    var c5 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty)).ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1]).ThenBy(x => x.Variables[2]).ThenBy(x => x.Variables[3])
                        .ThenBy(x => x.Variables[4]).Select(x => x.TestCase);
                    orderedCases.AddRange(c5);
                    break;
                case 6:
                    var c6 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty)).ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1]).ThenBy(x => x.Variables[2]).ThenBy(x => x.Variables[3])
                        .ThenBy(x => x.Variables[4]).ThenBy(x => x.Variables[5]).Select(x => x.TestCase);
                    orderedCases.AddRange(c6);
                    break;
                default:
                    orderedCases.AddRange(classTestCases.Select(x => x.TestCase));
                    break;
            }
        }
        catch (Exception ex)
        {
        }

        return orderedCases;
    }
}