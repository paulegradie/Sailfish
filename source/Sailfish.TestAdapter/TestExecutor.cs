using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
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

    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (tests is null) throw new Exception("Tests was null in the test case list!");
        if (runContext is null || frameworkHandle is null) throw new Exception("Wow more nulls");
        TestExecution.ExecuteTests(tests.ToList(), frameworkHandle, cancellationTokenSource.Token);
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null) throw new SailfishException("sources was null");

        var enumeratedSources = sources.ToList();
        if (runContext is null || frameworkHandle is null) throw new SailfishException($"Nulls encountered. runContext: {runContext}, frameworkHandle: {frameworkHandle}");

        var testCases = TestDiscovery.DiscoverTests(enumeratedSources, frameworkHandle);
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