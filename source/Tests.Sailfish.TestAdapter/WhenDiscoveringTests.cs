using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Discovery;
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
        var testCases = TestDiscovery.DiscoverTests(sources, new LoggerHelper()).ToList();
        testCases.Count.ShouldBe(7);
    }

    [TestMethod]
    public void TestCasesAreAssembledCorrect()
    {
        var sources = DllFinder.FindAllDllsRecursively();
        var testCases = TestDiscovery.DiscoverTests(sources, new LoggerHelper()).ToList();
        var aTestCase = testCases.First();
        aTestCase.DisplayName.ShouldBe("SimplePerfTest.ExecutionMethod(VariableA: 1, VariableB: 1000000)");
    }
}