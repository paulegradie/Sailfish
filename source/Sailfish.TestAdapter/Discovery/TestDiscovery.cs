using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sailfish.TestAdapter.Discovery;

public interface ITestDiscovery
{
    IEnumerable<TestCase> DiscoverTests(IEnumerable<string> sourceDllPaths, IMessageLogger logger);
}

internal class TestDiscovery : ITestDiscovery
{
    /// <summary>
    ///     This method should scan through the dlls provided, and extract each type that qualifies as a test type
    /// </summary>
    /// <param name="sourceDllPaths"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> sourceDllPaths, IMessageLogger logger)
    {
        var hasher = new HashWrapper();
        var testCases = new List<TestCase>();
        FileInfo? previousSearchDir = null;
        FileInfo? project = null;
        foreach (var sourceDllPath in sourceDllPaths.Distinct())
        {
            previousSearchDir = GetProjectOrThrow(logger, sourceDllPath, previousSearchDir, ref project);

            if (!TryFindPerformanceTestTypes(sourceDllPath, logger, out var performanceTestTypes)) continue;
            if (!TryFindAllFiles(project, logger, out var projectSourceCodeFilePaths)) continue;

            var classMetaDatas = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                    projectSourceCodeFilePaths,
                    performanceTestTypes,
                    nameof(SailfishAttribute).Replace(nameof(Attribute), ""),
                    nameof(SailfishMethodAttribute).Replace(nameof(Attribute), ""))
                .OrderBy(meta => meta.ClassFullName.Split(".").Last());

            foreach (var classMetaData in classMetaDatas)
            {
                AddTestCases(classMetaData, sourceDllPath, hasher, testCases);
            }
        }

        return testCases;
    }

    private static FileInfo? GetProjectOrThrow(IMessageLogger logger, string sourceDllPath, FileInfo? previousSearchDir, ref FileInfo? project)
    {
        var currentSearchDir = new FileInfo(sourceDllPath);
        if (WeArentBeingInefficient(previousSearchDir, currentSearchDir))
        {
            project = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(
                ".csproj",
                sourceDllPath,
                10);
            previousSearchDir = currentSearchDir;
        }

        if (project is not null)
        {
            return previousSearchDir;
        }

        const string msg = "Failed to discover the test project";
        logger.SendMessage(TestMessageLevel.Error, msg);
        throw new TestAdapterException(msg);
    }

    private static bool WeArentBeingInefficient(FileInfo? previousSearchDir, FileInfo currentSearchDir)
    {
        return previousSearchDir is null || (currentSearchDir.Directory is not null && previousSearchDir.Directory is not null &&
                                             currentSearchDir.Directory?.FullName != previousSearchDir.Directory?.FullName);
    }

    private static void AddTestCases(ClassMetaData classMetaData, string sourceDllPath, HashWrapper hasher, List<TestCase> testCases)
    {
        var classTestCases = TestCaseItemCreator
            .AssembleTestCases(classMetaData, sourceDllPath, hasher)
            .ToList();

        if (classTestCases.Count == 0) return;
        testCases.AddRange(classTestCases);
    }

    private static bool TryFindPerformanceTestTypes(string sourceDllPath, IMessageLogger logger, out Type[] performanceTestTypes)
    {
        try
        {
            performanceTestTypes = TypeLoader.LoadSailfishTestTypesFrom(sourceDllPath, logger);
            return performanceTestTypes.Length > 0;
        }
        catch
        {
            performanceTestTypes = [];
            return false;
        }
    }

    private static bool TryFindAllFiles(FileInfo project, IMessageLogger logger, out List<string> projectSourceCodeFilePaths)
    {
        projectSourceCodeFilePaths = DirectoryRecursion.FindAllFilesRecursively(
            project,
            "*.cs",
            logger,
            DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs);
        return projectSourceCodeFilePaths.Count > 0;
    }
}