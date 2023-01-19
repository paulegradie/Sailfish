using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.IO;
using System.Reflection;

namespace Sailfish.TestAdapter.Utils;

internal static class TestDiscovery
{
    private static readonly DirectoryRecursion DirRecursor = new();
    private static readonly TestCaseItemCreator TestCaseCreator = new();

    /// <summary>
    /// This method should scan through the dlls provided, and extract each type that qualifies as a test type
    /// Then, that 
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
            if (previousSearchDir is null || (currentSearchDir.Directory is not null && (previousSearchDir.Directory is not null) && (currentSearchDir.Directory?.FullName != previousSearchDir.Directory?.FullName)))
            {
                project = DirRecursor.RecurseUpwardsUntilFileIsFound(
                    ".csproj",
                    sourceDllPath,
                    10,
                    logger);
                previousSearchDir = currentSearchDir;
            }

            if (project is null) throw new Exception();


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

            var correspondingCsFiles = DirectoryRecursion.FindAllFilesRecursively(
                project,
                "*.cs",
                logger,
                DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs);
            if (correspondingCsFiles.Count == 0) continue;

            foreach (var perfTestType in perfTestTypes)
            {
                var fileAndContent = FindFileThatImplementsType(correspondingCsFiles, perfTestType);
                if (fileAndContent is null) throw new Exception($"Could not find corresponding file for {perfTestType.Name}!");
                var cases = TestCaseCreator.AssembleTestCases(perfTestType, fileAndContent.Content, fileAndContent.File, sourceDllPath, logger);
                testCases.AddRange(cases);
            }
        }

        return testCases;
    }

    private static FileAndContent? FindFileThatImplementsType(IEnumerable<string> files, MemberInfo type)
    {
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var className = $"class {type.Name}";
            if (content.Contains(className, StringComparison.InvariantCultureIgnoreCase))
            {
                return new FileAndContent(file, content);
            }
        }

        return null;
    }
}

internal class FileAndContent
{
    public FileAndContent(string file, string content)
    {
        File = file;
        Content = content;
    }

    public string File { get; }
    public string Content { get; }
}