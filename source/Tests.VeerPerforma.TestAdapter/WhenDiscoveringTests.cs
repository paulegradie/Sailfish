using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Tests.VeerPerforma.TestAdapter.Utils;
using VeerPerforma.TestAdapter.Utils;

namespace Tests.VeerPerforma.TestAdapter;

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