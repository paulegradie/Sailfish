using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using VeerPerforma.TestAdapter.Utils;

namespace VeerPerforma.TestAdapter;

[ExtensionUri(ExecutorUriString)]
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://vpexecutorv2";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);

    public bool Cancelled = false;

    public void RunTests(IEnumerable<string> sourceDlls, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        var testCases = new CustomTestDiscovery().DiscoverTests(sourceDlls);
        new TestExecution().ExecuteTests(testCases.ToList(), runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        new TestExecution().ExecuteTests(tests.ToList(), runContext, frameworkHandle);
    }

    public void Cancel()
    {
        Cancelled = true;
    }
}