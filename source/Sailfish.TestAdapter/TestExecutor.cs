using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Exceptions;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;

namespace Sailfish.TestAdapter;

[ExtensionUri(ExecutorUriString)]
// ReSharper disable once ClassNeverInstantiated.Global
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://sailfishexecutor/v1";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);

    private readonly object obj = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    public bool Cancelled;

    public void RunTests(IEnumerable<TestCase>? testsCases, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        var tests = testsCases?.ToList();
        if (tests is null) throw new Exception("Tests was null in the test case list!");
        if (runContext is null || frameworkHandle is null) throw new Exception("Wow more nulls");
        
        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Test Cases in Executor:\n{string.Join("\n-- ", tests.Select(x => x.DisplayName))}");
        TestExecution.ExecuteTests(tests.ToList(), frameworkHandle, cancellationTokenSource.Token);
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null) throw new SailfishException("No sources provided to run method. Sources was null");

        var enumeratedSources = sources.ToList();
        if (runContext is null || frameworkHandle is null) throw new SailfishException($"Nulls encountered. runContext: {runContext}, frameworkHandle: {frameworkHandle}");

        var testCases = TestDiscovery.DiscoverTests(enumeratedSources, frameworkHandle).ToList();
        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Test Cases in Executor:\n{string.Join("\n-- ", testCases.Select(x => x.DisplayName))}");
        TestExecution.ExecuteTests(testCases.ToList(), frameworkHandle, cancellationTokenSource.Token);
    }

    public void Cancel()
    {
        lock (obj)
        {
            cancellationTokenSource.Cancel();
            Cancelled = true;
        }
    }
}