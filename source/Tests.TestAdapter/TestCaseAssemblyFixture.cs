using System;
using System.IO;
using System.Linq;
using NSubstitute;
using Sailfish.Attributes;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Tests.TestAdapter.TestResources;
using Xunit;

namespace Tests.TestAdapter;

public class TestCaseAssemblyFixture
{
    private static string FindSpecificUniqueFile(string fileName)
    {
        // Start from the test assembly base directory to be robust under different runners
        var start = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", start, 10);
        var projectDir = Directory.GetParent(projFile.FullName)!.FullName;

        // Search for the file in the project directory
        var files = Directory.GetFiles(projectDir, fileName, SearchOption.AllDirectories);

        // Filter out files in bin/obj directories to avoid duplicates
        var filteredFiles = files.Where(f =>
            !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
            !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        if (filteredFiles.Count == 0)
        {
            throw new FileNotFoundException($"Could not find file '{fileName}' in project directory '{projectDir}'");
        }

        if (filteredFiles.Count > 1)
        {
            throw new InvalidOperationException(
                $"Found multiple instances of '{fileName}': {string.Join(", ", filteredFiles)}. Expected exactly one.");
        }

        return filteredFiles.Single();
    }

    [Fact]
    public void AllTestCasesAreMade()
    {
        var testResourceRelativePath = FindSpecificUniqueFile("TestResourceDoNotRename.cs");
        const string sourceDll = "C:/this/is/some/dll.dll";
        var sourceCache = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { testResourceRelativePath },
                new[] { typeof(SimplePerfTest) },
                nameof(SailfishAttribute).Replace("Attribute", string.Empty),
                nameof(SailfishMethodAttribute).Replace("Attribute", string.Empty))
            .ToList();

        var classMetaData = sourceCache.Single();
        var hasher = Substitute.For<IHashAlgorithm>();
        var result = TestCaseItemCreator
            .AssembleTestCases(classMetaData, sourceDll, hasher)
            .ToList();

        result.Count.ShouldBe(6);
    }
}