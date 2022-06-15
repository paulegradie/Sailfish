using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VeerPerforma.TestAdapter.Utils;

namespace VeerPerforma.TestAdapter
{
// https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md

    [FileExtension(".dll")]
    [FileExtension(".cs")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    public class TestDiscoverer : ITestDiscoverer
    {
        private readonly object obj = new object();

        public void DiscoverTests(IEnumerable<string> containers, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            ValidateArg.NotNull(containers, "containers");
            ValidateArg.NotNull(logger, "logger");
            ValidateArg.NotNull(discoverySink, "discoverySink");


            logger.SendMessage(TestMessageLevel.Informational, "THIS IS A TEST MESSAGE LEVEL");
            logger.SendMessage(TestMessageLevel.Warning, "THIS IS A WARNING");
            logger.SendMessage(TestMessageLevel.Error, "OOPSIE - THIS IS AN ERROR");

            lock (obj)
            {
                var testCases = new CustomTestDiscovery().DiscoverTests(containers);
                foreach (var testCase in testCases) discoverySink.SendTestCase(testCase);
            }
        }
    }
}