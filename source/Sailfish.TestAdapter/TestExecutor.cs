using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Diagnostics.Environment;
using Sailfish.Exceptions;
using Sailfish.Logging;
using Sailfish.Registration;
using Sailfish.Results;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Execution.EnvironmentHealth;
using Sailfish.TestAdapter.Execution.Aggregation;
using Sailfish.TestAdapter.Registrations;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter;

[ExtensionUri(ExecutorUriString)]
public class TestExecutor : ITestExecutor
{
    public const string ExecutorUriString = "executor://sailfishexecutor/v1";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly object _lockObject = new();
    private readonly ITestExecution _testExecution;
    public bool Cancelled;

    public TestExecutor()
    {
        _testExecution = new TestExecution();
    }

    public TestExecutor(ITestExecution testExecution)
    {
        _testExecution = testExecution;
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
        lock (_lockObject)
        {
            _cancellationTokenSource.Cancel();
            Cancelled = true;
        }

        _cancellationTokenSource.Dispose();
    }

    private void ExecuteTests(List<TestCase> testCases, IFrameworkHandle frameworkHandle)
    {
        frameworkHandle.EnableShutdownAfterTestRun = true;

        var services = new ServiceCollection();
        try
        {
            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();
            services.AddSailfish(runSettings);
            services.AddSailfishTestAdapter(frameworkHandle);

            var refTestType = RetrieveReferenceTypeForTestProject(testCases);
            SailfishTypeRegistrationUtility.InvokeRegistrationProviderCallbackMain(
                    services,
                    new[] { refTestType },
                    new[] { refTestType },
                    _cancellationTokenSource.Token)
                .Wait(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            HandleStartupException(frameworkHandle, testCases, ex);
            return;
        }

        var provider = services.BuildServiceProvider();

        try
        {
            // Environment health check (informational).
            try
            {
                var rs = provider.GetService<Sailfish.Contracts.Public.Models.IRunSettings>();

                // 1) Timer Calibration (session-level).
                try
                {
                    if (rs?.TimerCalibration is not false)
                    {
                        var timerSvc = provider.GetService<Sailfish.Execution.ITimerCalibrationService>();
                        var timerProv = provider.GetService<Sailfish.Execution.ITimerCalibrationResultProvider>();
                        if (timerSvc != null && timerProv != null)
                        {
                            var calib = timerSvc.CalibrateAsync(_cancellationTokenSource.Token)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();
                            timerProv.Current = calib;

                            var summary = $"Timer calibration: freq={calib.StopwatchFrequency} Hz, res≈{calib.ResolutionNs:F0} ns, baseline={calib.MedianTicks} ticks, RSD={calib.RsdPercent:F1}%, score={calib.JitterScore}/100";
                            frameworkHandle.SendMessage(TestMessageLevel.Informational, summary);
                            var log = provider.GetService<ILogger>();
                            log?.Log(LogLevel.Information, summary);
                        }
                    }
                }
                catch (Exception tex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Warning, $"Timer calibration failed: {tex.Message}");
                }

                // 2) Environment health check (informational).
                if (rs?.EnableEnvironmentHealthCheck is not false)
                {
                    var runner = provider.GetService<EnvironmentHealthCheckRunner>();
                    if (runner != null)
                    {
                        var ctx = new EnvironmentHealthCheckContext { TestAssemblyPath = testCases.FirstOrDefault()?.Source };
                        var result = runner.RunAsync(ctx, _cancellationTokenSource.Token)
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();

                        frameworkHandle.SendMessage(TestMessageLevel.Informational, result.Summary);

                        var reportProvider = provider.GetService<IEnvironmentHealthReportProvider>();
                        if (reportProvider is not null)
                        {
                            reportProvider.Current = result.Report;
                        }

                        var logger = provider.GetService<ILogger>();
                        logger?.Log(LogLevel.Information, result.Summary.TrimEnd());

                        // Initialize reproducibility manifest base (best-effort).
                        try
                        {
                            var manifestProvider = provider.GetService<IReproducibilityManifestProvider>();
                            if (manifestProvider != null && rs != null && manifestProvider.Current == null)
                            {
                                manifestProvider.Current = ReproducibilityManifest.CreateBase(rs, reportProvider?.Current);
                            }

                            try
                            {
                                var timerProv = provider.GetService<Sailfish.Execution.ITimerCalibrationResultProvider>();
                                var calib = timerProv?.Current;
                                if (manifestProvider != null && manifestProvider.Current != null && calib != null)
                                {
                                    manifestProvider.Current.TimerCalibration = ReproducibilityManifest.TimerCalibrationSnapshot.From(calib);
                                }
                            }
                            catch { /* best-effort */ }
                        }
                        catch { /* non-fatal */ }
                    }
                }
            }
            catch (Exception ex)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, $"Environment health check failed: {ex.Message}");
            }

            // Seed comparison-group sizes so the aggregator can fire each comparison exactly once when complete.
            var aggregator = provider.GetService<TestCompletionAggregator>();
            if (aggregator is not null) SeedComparisonGroups(aggregator, testCases);

            // Execute tests.
            _testExecution.ExecuteTests(testCases, provider, frameworkHandle, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            HandleStartupException(frameworkHandle, testCases, ex);
        }
        finally
        {
            // Flush the aggregator: publish any comparison group that never completed (deterministic, end-of-run).
            FlushComparisonAggregator(provider);

            // Dispose provider — releases all singletons and any other IDisposable services it owns.
            provider.Dispose();
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

    /// <summary>
    ///     Seeds the comparison aggregator with the number of test cases in each comparison group, as known from
    ///     discovery. This deterministic completeness signal lets each cross-method comparison fire exactly once,
    ///     the moment its group is whole.
    /// </summary>
    internal static void SeedComparisonGroups(TestCompletionAggregator aggregator, List<TestCase> testCases)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var testCase in testCases)
        {
            var group = testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonGroupProperty, null);
            if (string.IsNullOrEmpty(group)) continue;
            counts[group] = counts.TryGetValue(group, out var current) ? current + 1 : 1;
        }

        foreach (var (group, count) in counts)
        {
            aggregator.RegisterComparisonGroup(group, count);
        }
    }

    /// <summary>
    ///     Flushes the comparison aggregator at end of run: any comparison group that never reached its expected
    ///     count (e.g. a sibling crashed) is published with whatever successful members arrived. Best-effort.
    /// </summary>
    private static void FlushComparisonAggregator(IServiceProvider provider)
    {
        try
        {
            provider.GetService<TestCompletionAggregator>()
                ?.FlushAsync(CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            provider.GetService<ILogger>()?.Log(LogLevel.Warning, ex,
                "Failed to flush the comparison aggregator during cleanup. Error: {0}", ex.Message);
        }
    }

}
