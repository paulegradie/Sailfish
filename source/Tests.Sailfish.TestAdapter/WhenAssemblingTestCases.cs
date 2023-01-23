using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Tests.Sailfish.TestAdapter.TestResources;

namespace Tests.Sailfish.TestAdapter;

[TestClass]
public class WhenAssemblingTestCases
{
    public string FindSpecificUniqueFile(string fileName)
    {
        var refFile = Directory.GetFiles(".").First();

        var projFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", refFile, 10, Substitute.For<IMessageLogger>());
        var file = Directory.GetFiles(Directory.GetParent(projFile.FullName)!.FullName, fileName, SearchOption.AllDirectories).Single();
        return file;
    }

    [TestMethod]
    public void AllTestCasesAreMade()
    {
        var testResourceRelativePath = FindSpecificUniqueFile("TestResource.cs");
        var content = FileIo.ReadFileContents(testResourceRelativePath);

        var sourceDll = "C:/this/is/some/dll.dll";

        var result = TestCaseItemCreator.AssembleTestCases(typeof(SimplePerfTest), content, testResourceRelativePath, sourceDll, Substitute.For<IMessageLogger>()).ToList();

        result.Count.ShouldBe(6);
    }
}