using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Accord.Diagnostics;
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

    private readonly object lockObject = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    public bool Cancelled;

    public void RunTests(IEnumerable<TestCase>? testCases, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        Debug.Assert(frameworkHandle is not null);
        var tests = testCases?.ToList();
        if (tests is null) throw new Exception("Tests was null in the test case list!");
        if (runContext is null || frameworkHandle is null) throw new Exception("Wow more nulls");

        ExecuteTests(tests, frameworkHandle);
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null) throw new SailfishException("No sources provided to run method. Sources was null");
        var enumeratedSources = sources.ToList();
        if (runContext is null || frameworkHandle is null) throw new SailfishException($"Nulls encountered. runContext: {runContext}, frameworkHandle: {frameworkHandle}");

        var testCases = TestDiscovery.DiscoverTests(enumeratedSources, frameworkHandle).ToList();

        ExecuteTests(testCases, frameworkHandle);
    }

    private void ExecuteTests(List<TestCase> testCases, IFrameworkHandle frameworkHandle)
    {
        try
        {
            TestExecution.ExecuteTests(testCases, frameworkHandle, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            frameworkHandle.EnableShutdownAfterTestRun = true;
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Encountered exception while executing tests: {ex.Message}");
            foreach (var testCase in testCases)
            {
                var result = new TestResult(testCase);
                result.Outcome = TestOutcome.None;
                result.ErrorMessage = ex.Message;
                result.ErrorStackTrace = ex.StackTrace;
                result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, ex.Message));
                frameworkHandle.RecordResult(result);
                frameworkHandle.RecordEnd(testCase, TestOutcome.None);
            }
        }
    }

    public void Cancel()
    {
        lock (lockObject)
        {
            cancellationTokenSource.Cancel();
            Cancelled = true;
        }
    }
}