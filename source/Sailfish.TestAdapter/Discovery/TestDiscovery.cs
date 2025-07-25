﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;

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
                throw new TestAdapterException(msg);
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
                .OrderBy(meta => meta.ClassFullName.Split(".").Last());

            foreach (var classMetaData in classMetaDatas)
            {
                try
                {
                    var classTestCases = TestCaseItemCreator
                        .AssembleTestCases(classMetaData, sourceDllPath, hasher)
                        .ToList();

                    if (classTestCases.Count == 0) continue;
                    testCases.AddRange(classTestCases);
                }
                catch (Exception ex)
                {
                    logger.SendMessage(TestMessageLevel.Error,
                        $"Failed to discover tests for class {classMetaData.ClassFullName}: {ex.Message}");
                    // Continue with other classes instead of failing completely
                    continue;
                }
            }
        }

        return testCases;
    }
}