using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog.Core;
using VeerPerforma.TestAdapter.Utils;

namespace VeerPerforma.TestAdapter;

// https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md


[FileExtension(".dll")]
[FileExtension(".cs")]
[FileExtension(".exe")]
[DefaultExecutorUri(TestExecutor.ExecutorUriString)]
public class TestDiscoverer : ITestDiscoverer
{
    private Logger Serilogger => Logging.CreateLogger(nameof(TestDiscoverer));

    public void DiscoverTests(IEnumerable<string> containers, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        var testCases = containers.DiscoverTests(Serilogger);
        foreach (var testCase in testCases)
        {
            Serilogger.Information($"OMG WE FOUND A TEST: {testCase.DisplayName}");
            discoverySink.SendTestCase(testCase);
        }
    }
}