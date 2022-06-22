using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shouldly;
using Tests.VeerPerforma.TestAdapter.Utils;

namespace Tests.VeerPerforma.TestAdapter;

[TestClass]
public class WhenExecutingTests
{
    [TestMethod]
    public void FilteredTestsAreSuccessfullyDiscovered()
    {
        var executor = new TestExecution();
        var frameworkHandle = Substitute.For<IFrameworkHandle>();
        var context = Substitute.For<IRunContext>();

        var allDllsInThisProject = DllFinder.FindAllDllsRecursively();
        var testCases = new TestDiscovery().DiscoverTests(allDllsInThisProject).ToList();

        Should.NotThrow(() => executor.ExecuteTests(testCases.Take(1).ToList(), context, frameworkHandle));
    }
}