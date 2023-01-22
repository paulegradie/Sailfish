using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;

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
        tests = tests?.ToList();
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "We are hitting RunTests(IEnumerable<TestCase> tests");
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"The testcases were: {string.Join(", ", tests?.Select(x => x.DisplayName) ?? Array.Empty<string>())}");
        if (tests is null) throw new Exception("Tests was null in the test case list!");
        if (runContext is null || frameworkHandle is null) throw new Exception("Wow more nulls");
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"TestsCases: {JsonConvert.SerializeObject(tests)}");

        TestExecution.ExecuteTests(tests.ToList(), frameworkHandle);
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        sources = sources?.ToList();
        frameworkHandle?.SendMessage(TestMessageLevel.Informational,
            $"sources that were passed to this function: {string.Join(", ", sources?.ToArray() ?? Array.Empty<string>())}");
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, "We are hitting RunTests(IEnumerable<string> tests");

        if (sources is null) throw new Exception("Tests was null in the test case list!");

        var enumeratedSources = sources.ToList();
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"We are hitting RunTests(IEnumerable<string> sourceDlls - {string.Join(", ", enumeratedSources)}");
        if (runContext is null || frameworkHandle is null) throw new Exception("Wow more nulls");

        var testCases = TestDiscovery.DiscoverTests(enumeratedSources, frameworkHandle);
        frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"TestsCases: {JsonConvert.SerializeObject(testCases)}");
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