using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sailfish.TestAdapter.Utils;
using Shouldly;
using Tests.Sailfish.TestAdapter.Utils;

namespace Tests.Sailfish.TestAdapter;

[TestClass]
public class WhenDiscoveringTests
{
    [TestMethod]
    public void AllTestsAreDiscovered()
    {
        // sources is a list of dlls that we've discovered in this project
        var sources = DllFinder.FindAllDllsRecursively();

        // Assumes there is one valid test file.
        // And The discoverer tests will be those found from inside the 
        var testCases = TestDiscovery.DiscoverTests(sources).ToList();
        testCases.Count.ShouldBe(6);
    }
}