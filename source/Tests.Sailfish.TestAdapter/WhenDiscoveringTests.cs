using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sailfish.TestAdapter.Utils;
using Shouldly;
using Tests.VeerPerforma.TestAdapter.Utils;

namespace Tests.Sailfish.TestAdapter;

[TestClass]
public class WhenDiscoveringTests
{
    [TestMethod]
    public void AllTestsAreDiscovered()
    {
        var allDllsInThisProject = DllFinder.FindAllDllsRecursively();

        // Assumes there is one valid test file.
        var testCases = new TestDiscovery().DiscoverTests(allDllsInThisProject).ToList();
        testCases.Count.ShouldBe(6);
    }
}