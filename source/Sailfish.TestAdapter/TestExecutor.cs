using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Exceptions;
using Sailfish.Registration;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.TestProperties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sailfish.TestAdapter;

[ExtensionUri(ExecutorUriString)]
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://sailfishexecutor/v1";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);
    private readonly CancellationTokenSource cancellationTokenSource = new();

    private readonly object lockObject = new();
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

    public void Cancel()
    {
        lock (lockObject)
        {
            cancellationTokenSource.Cancel();
            Cancelled = true;
        }
    }

    private void ExecuteTests(List<TestCase> testCases, IFrameworkHandle frameworkHandle)
    {
        try
        {
            var runSettings = AdapterRunSettingsLoader.LoadAdapterRunSettings();
            var builder = new ContainerBuilder();
            builder.RegisterSailfishTypes(runSettings, new TestAdapterModule(frameworkHandle));

            var refTestType = RetrieveReferenceTypeForTestProject(testCases);
            SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
                    builder,
                    new[] { refTestType },
                    new[] { refTestType },
                    cancellationTokenSource.Token)
                .Wait(cancellationTokenSource.Token);

            using var container = builder.Build();
            TestExecution.ExecuteTests(testCases, container, frameworkHandle, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            frameworkHandle.EnableShutdownAfterTestRun = true;
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Encountered exception while executing tests: {ex.Message}");
            foreach (var testCase in testCases)
            {
                var result = new TestResult(testCase)
                {
                    Outcome = TestOutcome.Skipped,
                    ErrorMessage = ex.Message,
                    ErrorStackTrace = ex.StackTrace
                };
                result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, ex.Message));
                frameworkHandle.RecordResult(result);
                frameworkHandle.RecordEnd(testCase, TestOutcome.None);
            }
        }
    }

    public static Type RetrieveReferenceTypeForTestProject(IReadOnlyCollection<TestCase> testCases)
    {
        var assembly = Assembly.LoadFile(testCases.First().Source);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?

        var testTypeFullName = testCases
            .First()
            .GetPropertyHelper(SailfishManagedProperty.SailfishTypeProperty);

        var refTestType = assembly.GetType(testTypeFullName, true, true);
        if (refTestType is null) throw new Exception("First test type was null when starting test execution");
        return refTestType;
    }
}