using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.Attributes;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Tests.Sailfish.TestAdapter.TestResources;
using Xunit;

namespace Tests.Sailfish.TestAdapter;

public class WhenAssemblingTestCases
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
                nameof(SailfishAttribute).Replace("Attribute", ""),
                nameof(SailfishMethodAttribute).Replace("Attribute", ""))
            .ToList();

        var classMetaData = sourceCache.Single();
        var hasher = Substitute.For<IHashAlgorithm>();
        var result = TestCaseItemCreator
            .AssembleTestCases(typeof(SimplePerfTest), classMetaData, sourceDll, hasher, Substitute.For<IMessageLogger>())
            .ToList();

        result.Count.ShouldBe(6);
    }
}