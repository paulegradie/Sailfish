using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Sailfish.TestAdapter.Utils;
using Shouldly;
using Tests.Sailfish.TestAdapter.Utils;

namespace Tests.Sailfish.TestAdapter;

[TestClass]
public class WhenExecutingTests
{
    [TestMethod]
    public void FilteredTestsAreSuccessfullyDiscovered()
    {
        var frameworkHandle = Substitute.For<IFrameworkHandle>();

        var allDllsInThisProject = DllFinder.FindAllDllsRecursively();
        var testCases = TestDiscovery.DiscoverTests(allDllsInThisProject).ToList();

        Should.NotThrow(() => TestExecution.ExecuteTests(testCases.Take(1).ToList(), frameworkHandle));
    }
}