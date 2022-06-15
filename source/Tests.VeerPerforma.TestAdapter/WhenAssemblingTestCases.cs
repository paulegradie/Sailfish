using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Tests.VeerPerforma.TestAdapter.TestResources;
using VeerPerforma.TestAdapter.Utils;

namespace Tests.VeerPerforma.TestAdapter;

[TestClass]
public class WhenAssemblingTestCases
{
    public string FindSpecificUniqueFile(string fileName)
    {
        var recurse = new DirectoryRecursion();
        var refFile = Directory.GetFiles(".").First();

        var projFile = recurse.RecurseUpwardsUntilFileIsFound(".csproj", refFile, 10);
        var file = Directory.GetFiles(Directory.GetParent(projFile.FullName).FullName, fileName, SearchOption.AllDirectories).Single();
        return file;
    }

    [TestMethod]
    public void AllTestCasesAreMade()
    {
        var creator = new TestCaseItemCreator();
        var fileIo = new FileIo();

        var testResourceRelativePath = FindSpecificUniqueFile("TestResource.cs");
        var content = fileIo.ReadFileContents(testResourceRelativePath);

        var bag = new DataBag(testResourceRelativePath, content, typeof(SimplePerfTest));
        var sourceDll = "C:/this/is/some/dll.dll";

        var result = creator.AssembleTestCases(bag, sourceDll).ToList();

        result.Count.ShouldBe(6);
        var first = result.First();
        ;
    }
}