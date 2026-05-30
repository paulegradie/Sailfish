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
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
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

            // Start queue services if enabled.
            StartQueueServices(provider);

            // Execute tests.
            _testExecution.ExecuteTests(testCases, provider, frameworkHandle, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            HandleStartupException(frameworkHandle, testCases, ex);
        }
        finally
        {
            // Stop queue services if enabled.
            StopQueueServices(provider);

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
    ///     Starts queue services if the queue system is enabled. Handles startup failures gracefully by
    ///     logging errors but allowing test execution to continue with direct framework publishing as a
    ///     fallback mechanism.
    /// </summary>
    private void StartQueueServices(IServiceProvider provider)
    {
        try
        {
            StartQueueServicesAsync(provider, _cancellationTokenSource.Token)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            var logger = provider.GetService<ILogger>();
            logger?.Log(LogLevel.Warning, ex,
                "Failed to start queue services. Test execution will continue with direct framework publishing. Error: {0}",
                ex.Message);
        }
    }

    /// <summary>
    ///     Stops queue services if the queue system is enabled. Ensures all pending batches are processed
    ///     before stopping the queue services.
    /// </summary>
    private void StopQueueServices(IServiceProvider provider)
    {
        try
        {
            StopQueueServicesAsync(provider, _cancellationTokenSource.Token)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            var logger = provider.GetService<ILogger>();
            logger?.Log(LogLevel.Warning, ex,
                "Failed to stop queue services during cleanup. Error: {0}",
                ex.Message);
        }
    }

    /// <summary>
    ///     Asynchronously starts queue services including the queue manager and batching service.
    /// </summary>
    private async Task StartQueueServicesAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var queueConfiguration = provider.GetService<QueueConfiguration>();

        if (queueConfiguration == null || !queueConfiguration.IsEnabled)
        {
            return;
        }

        var logger = provider.GetService<ILogger>();
        logger?.Log(LogLevel.Information, "Starting queue services for test execution...");
        logger?.Log(LogLevel.Information, "Queue configuration - IsEnabled: {0}, EnableMethodComparison: {1}",
            queueConfiguration.IsEnabled, queueConfiguration.EnableMethodComparison);

        var queueManager = provider.GetService<TestCompletionQueueManager>();
        var batchingService = provider.GetService<ITestCaseBatchingService>();
        var timeoutHandler = provider.GetService<IBatchTimeoutHandler>();

        if (queueManager == null)
        {
            logger?.Log(LogLevel.Warning, "TestCompletionQueueManager not found in container. Queue services will not be started.");
            return;
        }

        if (batchingService == null)
        {
            logger?.Log(LogLevel.Warning, "ITestCaseBatchingService not found in container. Queue services will not be started.");
            return;
        }

        if (timeoutHandler == null)
        {
            logger?.Log(LogLevel.Warning, "IBatchTimeoutHandler not found in container. Timeout handling will not be available.");
        }

        await batchingService.StartAsync(cancellationToken).ConfigureAwait(false);
        logger?.Log(LogLevel.Debug, "Test case batching service started successfully");

        if (timeoutHandler != null)
        {
            await timeoutHandler.StartAsync(cancellationToken).ConfigureAwait(false);
            logger?.Log(LogLevel.Debug, "Batch timeout handler started successfully");
        }

        await queueManager.StartAsync(queueConfiguration, cancellationToken).ConfigureAwait(false);
        logger?.Log(LogLevel.Information, "Queue services started successfully");
    }

    /// <summary>
    ///     Asynchronously stops queue services with proper batch completion handling.
    /// </summary>
    private async Task StopQueueServicesAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var queueConfiguration = provider.GetService<QueueConfiguration>();
        if (queueConfiguration == null || !queueConfiguration.IsEnabled)
        {
            return;
        }

        var logger = provider.GetService<ILogger>();
        logger?.Log(LogLevel.Information, "Stopping queue services and completing pending batches...");

        var queueManager = provider.GetService<TestCompletionQueueManager>();
        var batchingService = provider.GetService<ITestCaseBatchingService>();
        var timeoutHandler = provider.GetService<IBatchTimeoutHandler>();

        if (queueManager == null && batchingService == null && timeoutHandler == null)
        {
            return;
        }

        // Queue shutdown uses a dedicated CTS bounded only by BatchCompletionTimeoutMs — NOT linked to
        // the execution cancellation token. If the test run was cancelled, we still want to flush
        // pending batches and gracefully stop the queue services; reusing the execution token would
        // short-circuit CompleteAsync/StopAsync the moment cancellation is requested and skip the drain.
        // The execution token is observed by 'cancellationToken' parameter recipients (the queue services
        // themselves can still cooperate with it if they choose), but we don't enforce it here.
        _ = cancellationToken; // intentionally not linked into shutdown — see comment above
        using var shutdownCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(queueConfiguration.BatchCompletionTimeoutMs));
        var shutdownToken = shutdownCts.Token;

        try
        {
            if (batchingService != null)
            {
                await batchingService.CompleteAsync(shutdownToken).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Test case batching service completed successfully");
            }

            if (queueManager != null)
            {
                await queueManager.CompleteAsync(shutdownToken).ConfigureAwait(false);
                await queueManager.StopAsync(shutdownToken).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Queue manager stopped successfully");
            }

            if (timeoutHandler != null)
            {
                await timeoutHandler.StopAsync(shutdownToken).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Batch timeout handler stopped successfully");
            }

            if (batchingService != null)
            {
                await batchingService.StopAsync(shutdownToken).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Test case batching service stopped successfully");
            }

            logger?.Log(LogLevel.Information, "Queue services stopped successfully");
        }
        catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
        {
            logger?.Log(LogLevel.Warning,
                "Batch completion timeout ({0}ms) expired. Proceeding with queue shutdown.",
                queueConfiguration.BatchCompletionTimeoutMs);

            // Force a final unconditional stop — if the shutdown timeout fired we don't want to compound
            // it with another cancellation on the StopAsync calls below.
            if (queueManager != null)
            {
                await queueManager.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            if (timeoutHandler != null)
            {
                await timeoutHandler.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            if (batchingService != null)
            {
                await batchingService.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
