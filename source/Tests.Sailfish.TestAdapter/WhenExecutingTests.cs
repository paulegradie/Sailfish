using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
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
        var testCases = TestDiscovery.DiscoverTests(allDllsInThisProject, Substitute.For<IMessageLogger>()).ToList();

        Should.NotThrow(() => TestExecution.ExecuteTests(testCases.Take(1).ToList(), frameworkHandle, CancellationToken.None));
    }
}