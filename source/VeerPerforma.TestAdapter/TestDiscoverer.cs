using System.Collections.Generic;
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
    private Logger Serilogger => Logging.CreateLogger(string.Join(".", "THIS_DAMN_HO", "txt"));

    public void DiscoverTests(IEnumerable<string> containers, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        Serilogger.Verbose("Starting test discovery");
        var testCases = containers.DiscoverTests(Serilogger);
        foreach (var testCase in testCases)
        {
            Serilogger.Verbose($"OMG WE FOUND A TEST: {testCase.DisplayName}");
            discoverySink.SendTestCase(testCase);
        }
    }
}