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
        var refFile = Directory.GetFiles(".").First();

        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", refFile, 10);
        var file = Directory.GetFiles(Directory.GetParent(projFile.FullName)!.FullName, fileName, SearchOption.AllDirectories).Single();
        return file;
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