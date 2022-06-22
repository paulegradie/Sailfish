using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.TestAdapter.Utils;
using Sailfish.Utils;

namespace Sailfish.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://veerperformaexecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private readonly object obj = new();

        public bool Cancelled;

        public void RunTests(IEnumerable<string> sourceDlls, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger.Verbose("We are hitting RunTests(IEnumerable<string> sourceDlls,");
            var testCases = new TestDiscovery().DiscoverTests(sourceDlls);
            new TestExecution().ExecuteTests(testCases.ToList(), runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger.Verbose("We are hitting RunTests(IEnumerable<TestCase> tests,");
            new TestExecution().ExecuteTests(tests.ToList(), runContext, frameworkHandle);
        }

        public void Cancel()
        {
            lock (obj)
            {
                Cancelled = true;
            }
        }
    }
}