using Sailfish.TestAdapter.Discovery;
using Shouldly;
using System.Linq;
using Tests.TestAdapter.Utils;
using Xunit;

namespace Tests.TestAdapter;

public class TestDiscoveryFixture
{
    [Fact]
    public void AllTestsAreDiscovered()
    {
        // sources is a list of dlls that we've discovered in this project
        var source = DllFinder.FindThisProjectsDllRecursively();

        // Assumes there is one valid test file.
        // And The discoverer tests will be those found from inside the
        var testCases = TestDiscovery.DiscoverTests(new[] { source }, new LoggerHelper()).ToList();
        testCases.Count.ShouldBe(18);
    }

    [Fact]
    public void TestCasesAreAssembledCorrect()
    {
        var source = DllFinder.FindThisProjectsDllRecursively();
        var testCases = TestDiscovery.DiscoverTests(new[] { source }, new LoggerHelper()).ToList();
        testCases.SingleOrDefault(x => x.FullyQualifiedName == "Tests.TestAdapter.TestResources.ExampleComponentTest.Interpolate(N: 1)").ShouldNotBeNull();
    }
}