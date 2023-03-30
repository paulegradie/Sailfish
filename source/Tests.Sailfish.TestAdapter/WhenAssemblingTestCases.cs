using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Tests.Sailfish.TestAdapter.TestResources;
using Xunit;

namespace Tests.Sailfish.TestAdapter;

public class WhenAssemblingTestCases
{
    public static string FindSpecificUniqueFile(string fileName)
    {
        var refFile = Directory.GetFiles(".").First();

        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", refFile, 10, Substitute.For<IMessageLogger>());
        var file = Directory.GetFiles(Directory.GetParent(projFile.FullName)!.FullName, fileName, SearchOption.AllDirectories).Single();
        return file;
    }

    [Fact]
    public void AllTestCasesAreMade()
    {
        var testResourceRelativePath = FindSpecificUniqueFile("TestResourceDoNotRename.cs");
        var content = File.ReadAllLines(testResourceRelativePath).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x)).ToList();

        const string sourceDll = "C:/this/is/some/dll.dll";

        var result = TestCaseItemCreator.AssembleTestCases(typeof(SimplePerfTest), content, testResourceRelativePath, sourceDll, Substitute.For<IMessageLogger>()).ToList();

        result.Count.ShouldBe(6);
    }
}