using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Serilog.Core;
using VeerPerforma.Attributes;
using VeerPerforma.Execution;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter.Utils;

internal static class TestDiscoveryExtensionMethod
{
    public static IEnumerable<TestCase> DiscoverTests(this IEnumerable<string> sourceFiles, Logger logger)
    {
        var sources = sourceFiles.ToList();
        logger.Information(string.Join(",", sources));

        var referenceFile = sources.ToList().First();

        var project = RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(referenceFile, 5);
        if (project is null) throw new Exception("Couldn't locate a csproj file in this project.");

        var assembly = Assembly.LoadFile(referenceFile);
        AppDomain.CurrentDomain.Load(assembly.GetName());

        var correspondingCsFiles = AlldotCSFilesWithinThis(project);
        var perfTestTypes = assembly                               // mvp only supports test discovery in current assembly 
            .GetTypes()
            .Where(x => x.HasAttribute<VeerPerformaAttribute>());

        var testCases = new List<TestCase>();

        var paramGridCreator = new ParameterGridCreator(new ParameterCombinator());

        foreach (var type in perfTestTypes)
        {
            var sourceFilePath = GetFilePathForThisType(type, correspondingCsFiles);
            if (sourceFilePath is null) continue;

            var fileContentArray = sourceFilePath.ReadFileContents();
            if (fileContentArray is null) continue;

            var parameterGrid = paramGridCreator.GenerateParameterGrid(type);
            var testCaseSet = AssembleClassTestCases(fileContentArray, type, sourceFilePath, parameterGrid);
            testCases.AddRange(testCaseSet);
        }

        return testCases;
    }

    private static IEnumerable<TestCase> AssembleClassTestCases(
        string[] fileContentArray,
        Type testType,
        string sourceFileLocation,
        (List<string>, IEnumerable<IEnumerable<int>>) parameterGrid)
    {
        var classNameLine = fileContentArray
            .Select(
                (line, index) => { return line.Contains(testType.Name) ? index : -1; })
            .Single(x => x >= 0);

        var method = testType.GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>().Single();
        var testCaseSet = parameterGrid.Item2.Select(
            paramSet =>
            {
                var randomId = Guid.NewGuid();
                var paramsCombo = DisplayNameHelper.CreateParamsDisplay(paramSet); //
                var fullyQualifiedName = string.Join(".", Assembly.CreateQualifiedName(Assembly.GetCallingAssembly().ToString(), testType.Name), paramsCombo);
                return new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, string.Join("/r", fileContentArray)) // a test case is a method
                {
                    CodeFilePath = sourceFileLocation,
                    DisplayName = DisplayNameHelper.CreateDisplayName(testType, method.Name, paramsCombo),
                    ExecutorUri = TestExecutor.ExecutorUri,
                    Id = randomId,
                    LineNumber = classNameLine,
                };
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

    private static string? GetFilePathForThisType(Type type, List<string> correspondingFiles)
    {
        return correspondingFiles.SingleOrDefault(file => file.EndsWith(type.Name));
    }

    private static List<string> AlldotCSFilesWithinThis(FileInfo originCsProjFile)
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

    public static bool ThereIsAParentDirectory(this DirectoryInfo dir, out DirectoryInfo parentDir)
    {
        if (dir.Parent is not null)
        {
            parentDir = dir.Parent;
            return dir.Parent.Exists;
        }

        parentDir = dir;
        return false;
    }

    public static FileInfo? RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(string sourceFile, int maxParentDirLevel)
    {
        if (maxParentDirLevel == 0) return null; // try and stave off disaster

        // get the directory of the source
        var dirName = Path.GetDirectoryName(sourceFile);
        if (dirName is null) return null;
        var currentDirectory = new DirectoryInfo(dirName);

        var csprojFile = currentDirectory.GetFiles("*.csproj").SingleOrDefault();
        if (csprojFile is null)
        {
            if (ThereIsAParentDirectory(currentDirectory, out var parentDir))
            {
                return RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(parentDir.FullName, maxParentDirLevel - 1);
            }
            else
            {
                return null;
            }
        }

        return csprojFile;
    }
}