using System.Linq;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Tests.Sailfish.TestAdapter.Utils;
using Xunit;

namespace Tests.Sailfish.TestAdapter;

public class WhenDiscoveringTests
{
    [Fact]
    public void AllTestsAreDiscovered()
    {
        // sources is a list of dlls that we've discovered in this project
        var source = DllFinder.FindThisProjectsDllRecursively();

        // Assumes there is one valid test file.
        // And The discoverer tests will be those found from inside the 
        var testCases = TestDiscovery.DiscoverTests(new[] { source }, new LoggerHelper()).ToList();
        testCases.Count.ShouldBe(16);
    }

    [Fact]
    public void TestCasesAreAssembledCorrect()
    {
        var source = DllFinder.FindThisProjectsDllRecursively();
        var testCases = TestDiscovery.DiscoverTests(new[] { source }, new LoggerHelper()).ToList();
        var aTestCase = testCases.First();
        aTestCase.DisplayName.ShouldBe("Interpolate(N: 1)");
    }
}