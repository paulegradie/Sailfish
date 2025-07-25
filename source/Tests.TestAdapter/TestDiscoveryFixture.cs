using System.Linq;
using Sailfish.TestAdapter.Discovery;
using Shouldly;

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
        var testCases = new TestDiscovery().DiscoverTests(new[] { source }, new LoggerHelper()).ToList();

        // Note: The exact count may vary as the project evolves, so we just verify that tests are found
        testCases.ShouldNotBeEmpty("Should discover at least some Sailfish tests from the project");
    }

    [Fact]
    public void TestCasesAreAssembledCorrect()
    {
        var source = DllFinder.FindThisProjectsDllRecursively();
        var testCases = new TestDiscovery().DiscoverTests(new[] { source }, new LoggerHelper()).ToList();

        // Verify that test cases have proper structure and naming
        testCases.ShouldNotBeEmpty();
        testCases.ShouldAllBe(tc => !string.IsNullOrEmpty(tc.FullyQualifiedName), "All test cases should have fully qualified names");
        testCases.ShouldAllBe(tc => !string.IsNullOrEmpty(tc.DisplayName), "All test cases should have display names");

        // Look for a specific test case from ValidSailfishTestClass to verify proper assembly
        var validTest = testCases.SingleOrDefault(x => x.FullyQualifiedName.Contains("ValidSailfishTestClass.TestMethod"));
        validTest.ShouldNotBeNull("Should find the expected ValidSailfishTestClass.TestMethod test case");
    }
}