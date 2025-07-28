using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Exceptions;
using Sailfish.Logging;
using Sailfish.Registration;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.Execution;
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
            // Start queue services if enabled
            StartQueueServices(container);

            // Execute tests
            testExecution.ExecuteTests(testCases, container, frameworkHandle, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            HandleStartupException(frameworkHandle, testCases, ex);
        }
        finally
        {
            // Stop queue services if enabled
            StopQueueServices(container);

            // Dispose container
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

    /// <summary>
    /// Starts queue services if the queue system is enabled.
    /// This method provides a synchronous wrapper around the asynchronous queue startup operations.
    /// </summary>
    /// <param name="container">The dependency injection container containing queue services.</param>
    /// <remarks>
    /// This method handles queue startup failures gracefully by logging errors but allowing
    /// test execution to continue with direct framework publishing as a fallback mechanism.
    /// The queue system is only started if QueueConfiguration.IsEnabled is true.
    /// </remarks>
    private void StartQueueServices(IContainer container)
    {
        try
        {
            StartQueueServicesAsync(container, cancellationTokenSource.Token)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            // Log error but continue - fallback to direct framework publishing
            var logger = container.ResolveOptional<ILogger>();
            logger?.Log(LogLevel.Warning, ex,
                "Failed to start queue services. Test execution will continue with direct framework publishing. Error: {0}",
                ex.Message);
        }
    }

    /// <summary>
    /// Stops queue services if the queue system is enabled.
    /// This method provides a synchronous wrapper around the asynchronous queue shutdown operations.
    /// </summary>
    /// <param name="container">The dependency injection container containing queue services.</param>
    /// <remarks>
    /// This method handles queue shutdown failures gracefully by logging errors but continuing
    /// with cleanup operations. It ensures all pending batches are processed before stopping
    /// the queue services. This method is called during test execution cleanup.
    /// </remarks>
    private void StopQueueServices(IContainer container)
    {
        try
        {
            StopQueueServicesAsync(container, cancellationTokenSource.Token)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            // Log error but continue with cleanup - we're in a finally block
            var logger = container.ResolveOptional<ILogger>();
            logger?.Log(LogLevel.Warning, ex,
                "Failed to stop queue services during cleanup. Error: {0}",
                ex.Message);
        }
    }

    /// <summary>
    /// Asynchronously starts queue services including the queue manager and batching service.
    /// </summary>
    /// <param name="container">The dependency injection container containing queue services.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the startup operation.</param>
    /// <returns>A task representing the asynchronous startup operation.</returns>
    /// <remarks>
    /// This method checks if the queue system is enabled via QueueConfiguration.IsEnabled.
    /// If enabled, it starts the batching service first, then the queue manager.
    /// The startup sequence ensures proper initialization order for the queue infrastructure.
    /// </remarks>
    private async Task StartQueueServicesAsync(IContainer container, CancellationToken cancellationToken)
    {
        // Check if queue system is enabled
        var queueConfiguration = container.ResolveOptional<QueueConfiguration>();
        if (queueConfiguration == null || !queueConfiguration.IsEnabled)
        {
            return; // Queue system is disabled, nothing to start
        }

        var logger = container.ResolveOptional<ILogger>();
        logger?.Log(LogLevel.Information, "Starting queue services for test execution...");

        // Resolve queue services
        var queueManager = container.ResolveOptional<TestCompletionQueueManager>();
        var batchingService = container.ResolveOptional<ITestCaseBatchingService>();
        var timeoutHandler = container.ResolveOptional<IBatchTimeoutHandler>();

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

        // Start batching service first
        await batchingService.StartAsync(cancellationToken).ConfigureAwait(false);
        logger?.Log(LogLevel.Debug, "Test case batching service started successfully");

        // Start timeout handler if available
        if (timeoutHandler != null)
        {
            await timeoutHandler.StartAsync(cancellationToken).ConfigureAwait(false);
            logger?.Log(LogLevel.Debug, "Batch timeout handler started successfully");
        }

        // Start queue manager
        await queueManager.StartAsync(queueConfiguration, cancellationToken).ConfigureAwait(false);
        logger?.Log(LogLevel.Information, "Queue services started successfully");
    }

    /// <summary>
    /// Asynchronously stops queue services with proper batch completion handling.
    /// </summary>
    /// <param name="container">The dependency injection container containing queue services.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the shutdown operation.</param>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    /// <remarks>
    /// This method ensures all pending batches are processed before stopping the queue services.
    /// It uses the configured batch completion timeout to wait for batch processing to complete.
    /// The shutdown sequence stops the queue manager first, then the batching service.
    /// </remarks>
    private async Task StopQueueServicesAsync(IContainer container, CancellationToken cancellationToken)
    {
        // Check if queue system is enabled
        var queueConfiguration = container.ResolveOptional<QueueConfiguration>();
        if (queueConfiguration == null || !queueConfiguration.IsEnabled)
        {
            return; // Queue system is disabled, nothing to stop
        }

        var logger = container.ResolveOptional<ILogger>();
        logger?.Log(LogLevel.Information, "Stopping queue services and completing pending batches...");

        // Resolve queue services
        var queueManager = container.ResolveOptional<TestCompletionQueueManager>();
        var batchingService = container.ResolveOptional<ITestCaseBatchingService>();
        var timeoutHandler = container.ResolveOptional<IBatchTimeoutHandler>();

        if (queueManager == null && batchingService == null && timeoutHandler == null)
        {
            return; // No queue services to stop
        }

        // Create timeout for batch completion
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(queueConfiguration.BatchCompletionTimeoutMs));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Complete batching service first to finalize all pending batches
            if (batchingService != null)
            {
                await batchingService.CompleteAsync(combinedCts.Token).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Test case batching service completed successfully");
            }

            // Complete and stop queue manager
            if (queueManager != null)
            {
                await queueManager.CompleteAsync(combinedCts.Token).ConfigureAwait(false);
                await queueManager.StopAsync(combinedCts.Token).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Queue manager stopped successfully");
            }

            // Stop timeout handler
            if (timeoutHandler != null)
            {
                await timeoutHandler.StopAsync(combinedCts.Token).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Batch timeout handler stopped successfully");
            }

            // Stop batching service
            if (batchingService != null)
            {
                await batchingService.StopAsync(combinedCts.Token).ConfigureAwait(false);
                logger?.Log(LogLevel.Debug, "Test case batching service stopped successfully");
            }

            logger?.Log(LogLevel.Information, "Queue services stopped successfully");
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            logger?.Log(LogLevel.Warning,
                "Batch completion timeout ({0}ms) expired. Proceeding with queue shutdown.",
                queueConfiguration.BatchCompletionTimeoutMs);

            // Force stop services even if timeout occurred
            if (queueManager != null)
            {
                await queueManager.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            if (timeoutHandler != null)
            {
                await timeoutHandler.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            if (batchingService != null)
            {
                await batchingService.StopAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}