using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Tests.Sailfish.TestAdapter.Utils;
using Xunit;

namespace Tests.Sailfish.TestAdapter;

public class WhenExecutingTests
{
    [Fact]
    public void FilteredTestsAreSuccessfullyDiscovered()
    {
        var frameworkHandle = Substitute.For<IFrameworkHandle>();

        var projectDll = DllFinder.FindThisProjectsDllRecursively();
        var testCases = TestDiscovery.DiscoverTests(new[] { projectDll }, Substitute.For<IMessageLogger>()).ToList();

        Should.NotThrow(() => TestExecution.ExecuteTests(testCases.Take(1).ToList(), frameworkHandle, CancellationToken.None));
    }

    [Fact]
    public void TestCasesAreExecutedCorrectly()
    {
        var frameworkHandle = Substitute.For<IFrameworkHandle>();

        var source = DllFinder.FindThisProjectsDllRecursively();
        var testCases = TestDiscovery.DiscoverTests(new[] { source }, new LoggerHelper()).ToList();

        Should.NotThrow(() => TestExecution.ExecuteTests(testCases, frameworkHandle, CancellationToken.None));
    }
}