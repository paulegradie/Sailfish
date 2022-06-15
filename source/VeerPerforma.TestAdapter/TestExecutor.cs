using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using VeerPerforma.TestAdapter.Utils;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://veerperformaexecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public bool Cancelled;

        public void RunTests(IEnumerable<string> sourceDlls, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger.Verbose("We are hitting RunTests(IEnumerable<string> sourceDlls,");
            var testCases = new CustomTestDiscovery().DiscoverTests(sourceDlls);
            new TestExecution().ExecuteTests(testCases.ToList(), runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger.Verbose("We are hitting RunTests(IEnumerable<TestCase> tests,");
            new TestExecution().ExecuteTests(tests.ToList(), runContext, frameworkHandle);
        }

        public void Cancel()
        {
            Cancelled = true;
        }
    }
}