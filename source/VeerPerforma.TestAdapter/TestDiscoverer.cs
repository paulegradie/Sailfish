using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VeerPerforma.TestAdapter.Utils;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter;

// https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md


[FileExtension(".dll")]
[FileExtension(".cs")]
[FileExtension(".exe")]
[DefaultExecutorUri(TestExecutor.ExecutorUriString)]
public class TestDiscoverer : ITestDiscoverer
{

    public void DiscoverTests(IEnumerable<string> containers, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        // CustomLoggerOKAY Serilogger = CustomLoggerOKAY.CreateLogger($"C:\\Users\\paule\\code\\VeerPerformaRelated\\TestingLogs\\CustomLogger_DISCOVERER_Logs-{Guid.NewGuid().ToString()}.txt");

        // Serilogger.Verbose("Starting test discovery");
        var testCases = containers.DiscoverTests(null);
        foreach (var testCase in testCases)
        {
            // Serilogger.Verbose($"OMG WE FOUND A TEST: {testCase.DisplayName}");
            discoverySink.SendTestCase(testCase);
        }
    }
}