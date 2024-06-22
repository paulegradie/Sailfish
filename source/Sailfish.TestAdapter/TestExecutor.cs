using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Exceptions;
using Sailfish.Registration;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Registrations;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter;

[ExtensionUri(ExecutorUriString)]
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://sailfishexecutor/v1";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);
    private readonly CancellationTokenSource cancellationTokenSource = new();

    private readonly object lockObject = new();
    private readonly ITestExecution testExecution;
    public bool Cancelled;

    public TestExecutor()
    {
        testExecution = new TestExecution();
    }

    public TestExecutor(ITestExecution testExecution)
    {
        this.testExecution = testExecution;
    }

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (sources is null) throw new SailfishException("No sources provided to run method. Sources was null");
        var enumeratedSources = sources.ToList();
        if (runContext is null || frameworkHandle is null)
            throw new SailfishException(
                $"Nulls encountered. runContext: {runContext}, frameworkHandle: {frameworkHandle}");

        var testCases = new TestDiscovery().DiscoverTests(enumeratedSources, frameworkHandle).ToList();

        RunTests(testCases, runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<TestCase>? testCases, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        Debug.Assert(frameworkHandle is not null);
        var tests = testCases?.ToList() ?? throw new TestAdapterException("Tests was null in the test case list!");
        if (runContext is null || frameworkHandle is null) throw new TestAdapterException("Wow more nulls");

        ExecuteTests(tests, frameworkHandle);
    }

    public void Cancel()
    {
        lock (lockObject)
        {
            cancellationTokenSource.Cancel();
            Cancelled = true;
        }

        cancellationTokenSource.Dispose();
    }

    private void ExecuteTests(List<TestCase> testCases, IFrameworkHandle frameworkHandle)
    {
        frameworkHandle.EnableShutdownAfterTestRun = true;

        var builder = new ContainerBuilder();
        try
        {
            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();
            builder.RegisterSailfishTypes(runSettings, new TestAdapterRegistrations(frameworkHandle));

            var refTestType = RetrieveReferenceTypeForTestProject(testCases);
            SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
                    builder,
                    new[] { refTestType },
                    new[] { refTestType },
                    cancellationTokenSource.Token)
                .Wait(cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            HandleStartupException(frameworkHandle, testCases, ex);
            return;
        }

        var container = builder.Build();

        try
        {
            testExecution.ExecuteTests(testCases, container, frameworkHandle, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            HandleStartupException(frameworkHandle, testCases, ex);
        }
        finally
        {
            container.Dispose();
        }
    }

    private static void HandleStartupException(ITestExecutionRecorder frameworkHandle, List<TestCase> testCases, Exception ex)
    {
        frameworkHandle.SendMessage(
            TestMessageLevel.Warning, // error level will fail the test suite
            $"Encountered exception while executing tests: {ex.Message}");
        foreach (var testCase in testCases)
        {
            var result = new TestResult(testCase) { Outcome = TestOutcome.Skipped, ErrorMessage = ex.Message, ErrorStackTrace = ex.StackTrace };
            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, ex.Message));
            frameworkHandle.RecordResult(result);
            frameworkHandle.RecordEnd(testCase, TestOutcome.Skipped);
        }
    }

    internal static Type RetrieveReferenceTypeForTestProject(IReadOnlyCollection<TestCase> testCases)
    {
        var assembly = Assembly.LoadFile(testCases.First().Source);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?

        var testTypeFullName = testCases
            .First()
            .GetPropertyHelper(SailfishManagedProperty.SailfishTypeProperty);

        var refTestType = assembly.GetType(testTypeFullName, true, true);
        if (refTestType is null) throw new TestAdapterException("First test type was null when starting test execution");
        return refTestType;
    }
}