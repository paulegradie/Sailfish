using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VeerPerforma.Attributes.TestHarness;
using VeerPerforma.Execution;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter.ExtensionMethods;

internal static class TestDiscoveryExtensionMethod
{
    public static IEnumerable<TestCase> DiscoverTests(this IEnumerable<string> sourceFiles)
    {
        var sources = sourceFiles.ToList();
        var referenceFile = sources.ToList().First();

        var project = RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(referenceFile, 5);
        if (project is null) throw new Exception("Couldn't locate a csproj file in this project.");

        var assembly = Assembly.LoadFile(referenceFile);
        AppDomain.CurrentDomain.Load(assembly.GetName());

        var correspondingCsFiles = AlldotCSFilesWithinThis(project);
        var perfTestTypes = assembly
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
                return new TestCase // a test case is a method
                {
                    CodeFilePath = sourceFileLocation,
                    DisplayName = DisplayNameHelper.CreateDisplayName(testType, method.Name, paramsCombo),
                    ExecutorUri = TestExecutor.ExecutorUri,
                    FullyQualifiedName = string.Join(".", Assembly.CreateQualifiedName(Assembly.GetCallingAssembly().ToString(), testType.Name), paramsCombo),
                    Id = randomId,
                    LineNumber = classNameLine,
                    // LocalExtensionData = new object(),
                    Source = string.Join("/r", fileContentArray),
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

    private static bool ThereIsAParentDirectory(DirectoryInfo dir, out DirectoryInfo parentDir)
    {
        if (dir.Parent is not null)
        {
            parentDir = dir.Parent;
            return dir.Parent.Exists;
        }

        parentDir = dir;
        return false;
    }

    private static FileInfo? RecurseUpwardsUntilTheProjectFileIsFoundStartingFromThis(string source, int maxParentDirLevel)
    {
        if (maxParentDirLevel == 0) return null;

        // get the directory of the source
        var dirName = Path.GetDirectoryName(source);
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