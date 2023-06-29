using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;

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
                    10,
                    logger);
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
                logger.SendMessage(TestMessageLevel.Warning, $"Skipping {sourceDllPath} ");
                continue;
            }

            if (perfTestTypes.Length == 0) continue;

            var projectSourceCodeFilePaths = DirectoryRecursion.FindAllFilesRecursively(
                project,
                "*.cs",
                logger,
                DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs);
            if (projectSourceCodeFilePaths.Count == 0) continue;

            var sourceCache = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                    projectSourceCodeFilePaths,
                    nameof(SailfishAttribute).Replace(nameof(Attribute), ""),
                    nameof(SailfishMethodAttribute).Replace(nameof(Attribute), ""))
                .OrderBy(meta => meta.ClassName);

            foreach (var classMetaData in sourceCache)
            {
                var index = perfTestTypes
                    .Select(t => t.Name)
                    .ToList()
                    .IndexOf(classMetaData.ClassName);
                var perfTestType = perfTestTypes[index];

                var classTestCases = TestCaseItemCreator.AssembleTestCases(perfTestType, classMetaData, sourceDllPath, logger);
                testCases.AddRange(classTestCases);
            }
        }

        return testCases;
    }
}