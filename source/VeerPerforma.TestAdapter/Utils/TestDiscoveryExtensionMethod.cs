using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Serilog.Core;
using VeerPerforma.Attributes;
using VeerPerforma.Execution;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter.Utils;

public class DataMap
{
    public DataMap()
    {
    }

    public string? FilePath { get; set; }
    public string? ContentString { get; set; }
    public Type? Type { get; set; }

    // public static TestProperty TestProp = GetTestProperty();
    //
    // static TestProperty GetTestProperty()
    // {
    //     return TestProperty.Register("VeerPerformaTestCase", "VeerPerforma Test Case", typeof(Assembly), typeof(DataMap));
    // } 
}

internal static class TestDiscoveryExtensionMethod
{
    public static IParameterGridCreator ParameterGridCreator = new ParameterGridCreator(new ParameterCombinator());

    public static IEnumerable<TestCase> DiscoverTests(this IEnumerable<string> sourceDlls, Logger logger)
    {
        logger.Verbose("Entering into the DiscoverTests method!");
        var sourceDllPaths = sourceDlls.ToList();
        var referenceFile = sourceDllPaths.ToList().First();

        logger.Verbose("About to iterate the dlls");
        var testCases = new List<TestCase>();
        foreach (var sourceDllPath in sourceDllPaths)
        {
            logger.Verbose("File sources: {0}", sourceDllPath.ToString());
            logger.Verbose("Ref File: {0}", referenceFile);
            logger.Verbose("Attempting to discover project...");
            var project = RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(".csproj", referenceFile, 5, logger);

            if (project is null) throw new Exception("Couldn't locate a csproj file in this project.");
            logger.Verbose("Project that was found: {0}", project.FullName);

            var assembly = Assembly.LoadFile(sourceDllPath);
            AppDomain.CurrentDomain.Load(assembly.GetName());
            logger.Verbose("Assembly: {0}", assembly.FullName);

            var correspondingCsFiles = AllDotCSFilesWithinThis(project);
            foreach (var csFile in correspondingCsFiles)
            {
                logger.Verbose("Corresponding .cs files in this assembly project");
                logger.Verbose("{0}", csFile);
            }

            var perfTestTypes = assembly // mvp only supports test discovery in current assembly 
                .GetTypes()
                .Where(x => x.HasAttribute<VeerPerformaAttribute>())
                .ToArray();
            if (perfTestTypes.Length < 1) throw new Exception("No perf test types found");

            foreach (var testType in perfTestTypes)
            {
                logger.Verbose("Perf tests: {0}", testType.Name);
            }

            logger.Verbose("---------------------");
            logger.Verbose("---------------------");
            logger.Verbose("---------------------");
            logger.Verbose("Beginning assembly of test cases!");

            logger.Verbose("Creating filePathTOContentStringMap");


            var bags = new List<DataMap>(); // filePath : content string
            foreach (var filePath in correspondingCsFiles)
            {
                var propertyBag = new DataMap();
                var content = filePath.ReadFileContents();
                if (content is null) throw new IOException($"Failed to read: {filePath}");
                var contentString = string.Join("\r", content);
                var testType = perfTestTypes.SingleOrDefault(x => contentString.Contains(x.Name));
                if (testType is null) throw new Exception("Failed to find type name in file contents");

                propertyBag.ContentString = contentString;
                propertyBag.FilePath = filePath;
                propertyBag.Type = testType;

                bags.Add(propertyBag);
            }

            if (bags.Count < 1) throw new Exception("Failed to find any test type file paths");

            var currentTestCases = bags.SelectMany(bag => AssembleClassTestCases(bag, sourceDllPath, logger));
            testCases.AddRange(currentTestCases);
        }

        return testCases;
    }

    private static IEnumerable<TestCase> AssembleClassTestCases(DataMap bag, string sourceDll, Logger logger)
    {
        var classNameLine = bag.ContentString.Split("\r")
            .Select(
                (line, index) => { return line.Contains(bag.Type.Name) ? index : -1; })
            .Single(x => x >= 0);

        var method = bag.Type.GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>().Single(); // should never be null
        var parameterGrid = ParameterGridCreator.GenerateParameterGrid(bag.Type);

        var testCaseSet = parameterGrid.Item2.Select(
            paramz =>
            {
                var paramSet = paramz.ToList();

                logger.Verbose("Param set for {Method}: {ParamSet}", method.Name, string.Join(", ", paramSet.Select(x => x.ToString())));
                var randomId = Guid.NewGuid();
                var paramsDisplayName = DisplayNameHelper.CreateParamsDisplay(paramSet);
                var fullyQualifiedName = string.Join(".", Assembly.CreateQualifiedName(Assembly.GetCallingAssembly().ToString(), bag.Type.Name), paramsDisplayName);
                var testCase = new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, sourceDll) // a test case is a method
                {
                    CodeFilePath = bag.FilePath,
                    DisplayName = DisplayNameHelper.CreateDisplayName(bag.Type, method.Name, paramsDisplayName),
                    ExecutorUri = TestExecutor.ExecutorUri,
                    Id = randomId,
                    LineNumber = classNameLine,
                };

                return testCase;
            });

        return testCaseSet;
    }

    private static string[]? ReadFileContents(this string sourceFile)
    {
        try
        {
            using var fileStream = new StreamReader(sourceFile);
            var content = fileStream.ReadToEnd();
            return content.Split("\r");
        }
        catch
        {
            return null;
        }
    }

    private static List<string> AllDotCSFilesWithinThis(FileInfo originCsProjFile)
    {
        var allDotCSFilesInProject = Directory
            .GetFiles(Path.GetDirectoryName(originCsProjFile.FullName)!, "*.cs", SearchOption.AllDirectories);
        return allDotCSFilesInProject.Where(FilePathDoesNotContainBinOrObjDirs).ToList();
    }

    private static bool FilePathDoesNotContainBinOrObjDirs(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return !(path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}"));
    }

    public static bool ThereIsAParentDirectory(this DirectoryInfo dir, Logger logger, out DirectoryInfo parentDir)
    {
        if (dir.Parent is not null)
        {
            logger.Verbose("CurrentDir: {0} --- Parent Directory: {1}", dir.Name, dir.Parent);
            parentDir = dir.Parent;
            return dir.Parent.Exists;
        }

        parentDir = dir;
        return false;
    }

    public static FileInfo? RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(string suffixToMatch, string sourceFile, int maxParentDirLevel, Logger logger)
    {
        if (maxParentDirLevel == 0) return null; // try and stave off disaster

        // get the directory of the source
        var dirName = Path.GetDirectoryName(sourceFile);
        logger.Verbose("The Current Dir at level {0} was {1}", maxParentDirLevel.ToString(), dirName);
        if (dirName is null) return null;

        var csprojFile = Directory.GetFiles(dirName).Where(x => x.EndsWith(suffixToMatch)).SingleOrDefault();
        if (csprojFile is null)
        {
            if (ThereIsAParentDirectory(new DirectoryInfo(sourceFile), logger, out var parentDir))
            {
                return RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(suffixToMatch, parentDir.FullName, maxParentDirLevel - 1, logger);
            }
            else
            {
                return null;
            }
        }

        logger.Verbose("Found the proj file! Finally! {0}", csprojFile);
        return new FileInfo(csprojFile);
    }

    public static string Join<T>(this T enumerable, string sep = ", ") where T : IEnumerable
    {
        return string.Join(sep, enumerable);
    }
}