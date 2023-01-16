using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.TestAdapter.Utils;

namespace Sailfish.TestAdapter;

[ExtensionUri(ExecutorUriString)]
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://sailfishexecutor/v1";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);

    private readonly object obj = new();

    public bool Cancelled;


    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        CustomLogger.Verbose("We are hitting RunTests(IEnumerable<TestCase> tests,");
        if (tests is null) throw new Exception("Tests was null in the test case list!");
        if (runContext is null || frameworkHandle is null) throw new Exception("Wow more nulls");

        TestExecution.ExecuteTests(tests.ToList(), frameworkHandle);
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null) throw new Exception("Tests was null in the test case list!");

        var enumeratedSources = sources.ToList();
        CustomLogger.Verbose("We are hitting RunTests(IEnumerable<string> sourceDlls - {DLLS}", string.Join(", ", enumeratedSources));
        if (runContext is null || frameworkHandle is null) throw new Exception("Wow more nulls");

        var testCases = TestDiscovery.DiscoverTests(enumeratedSources);
        TestExecution.ExecuteTests(testCases.ToList(), frameworkHandle);
    }

    public void Cancel()
    {
        lock (obj)
        {
            Cancelled = true;
        }
    }
}