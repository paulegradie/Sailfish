using System.Linq;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Diagnostics.Environment;
using Sailfish.Logging;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Execution.EnvironmentHealth;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Handlers.TestCaseEvents;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Sailfish.TestAdapter.Queue.Processors;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;

namespace Sailfish.TestAdapter.Registrations;

/// <summary>
///     Adds the TestAdapter-specific services on top of the core Sailfish registrations (which the caller
///     must have already added via <c>services.AddSailfish(runSettings)</c>).
/// </summary>
internal static class TestAdapterRegistrations
{
    public static IServiceCollection AddSailfishTestAdapter(this IServiceCollection services, IFrameworkHandle? frameworkHandle)
    {
        if (frameworkHandle is not null)
        {
            services.AddSingleton(frameworkHandle);
        }

        services.AddTransient<ITestAdapterExecutionProgram, TestAdapterExecutionProgram>();
        services.AddTransient<ITestAdapterExecutionEngine, TestAdapterExecutionEngine>();

        // AdapterSailDiff exposes two interface views over the same implementation type — register both.
        services.AddTransient<ISailDiffInternal, AdapterSailDiff>();
        services.AddTransient<IAdapterSailDiff, AdapterSailDiff>();

        services.AddTransient<IScaleFishInternal, AdapterScaleFish>();
        services.AddTransient<IAdapterScaleFish, AdapterScaleFish>();

        services.AddSingleton<ITestCaseCountPrinter, TestCaseCountPrinter>();
        services.AddSingleton<ITestFrameworkWriter, TestFrameworkWriter>();
        services.AddSingleton<IEnvironmentHealthChecker, EnvironmentHealthChecker>();
        services.AddSingleton<EnvironmentHealthCheckRunner>();

        services.AddTransient<ISailDiffTestOutputWindowMessageFormatter, SailDiffTestOutputWindowMessageFormatter>();
        services.AddTransient<ISailfishConsoleWindowFormatter, SailfishConsoleWindowFormatter>();

        services.AddTransient<INotificationHandler<TestCaseStartedNotification>, TestCaseStartedNotificationHandler>();
        services.AddTransient<INotificationHandler<TestCaseCompletedNotification>, TestCaseCompletedNotificationHandler>();
        services.AddTransient<INotificationHandler<TestCaseDisabledNotification>, TestCaseDisabledNotificationHandler>();
        services.AddTransient<INotificationHandler<TestCaseExceptionNotification>, TestCaseExceptionNotificationHandler>();

        services.AddTransient<INotificationHandler<FrameworkTestCaseEndNotification>, FrameworkTestCaseEndNotificationHandler>();

        // Register queue services conditionally based on configuration.
        RegisterQueueServices(services);

        return services;
    }

    /// <summary>
    ///     Registers queue services in the DI container based on queue configuration. This method implements
    ///     conditional registration to ensure queue services are only registered when the queue system is
    ///     enabled, maintaining backward compatibility with existing functionality.
    /// </summary>
    private static void RegisterQueueServices(IServiceCollection services)
    {
        var queueConfiguration = GetQueueConfiguration();

        // Always register the configuration for potential use by other services.
        services.AddSingleton(queueConfiguration);

        // Only register queue services if the queue system is enabled.
        if (!queueConfiguration.IsEnabled)
        {
            return;
        }

        RegisterCoreQueueServices(services);
        RegisterBatchingServices(services);
        RegisterQueueProcessors(services, queueConfiguration);
    }

    /// <summary>
    ///     Registers core queue infrastructure services including the queue, publisher, factory, manager,
    ///     and consumer services. These services form the foundation of the intercepting queue architecture
    ///     that enables asynchronous processing and batch analysis.
    /// </summary>
    private static void RegisterCoreQueueServices(IServiceCollection services)
    {
        // Core queue implementation — singleton to maintain state during test execution.
        services.AddSingleton<ITestCompletionQueue>(sp =>
            new InMemoryTestCompletionQueue(sp.GetRequiredService<QueueConfiguration>()));

        // Queue publisher — transient, lightweight.
        services.AddTransient<ITestCompletionQueuePublisher, TestCompletionQueuePublisher>();

        // Queue manager — singleton; injected with all processors. Use a factory so the processor IEnumerable
        // is resolved fresh from the container.
        services.AddSingleton(sp =>
        {
            var queue = sp.GetRequiredService<ITestCompletionQueue>();
            var processors = sp.GetServices<ITestCompletionQueueProcessor>().ToArray();
            var logger = sp.GetRequiredService<ILogger>();
            return new TestCompletionQueueManager(queue, processors, logger);
        });

        // Queue consumer — singleton to maintain processing state.
        services.AddSingleton<TestCompletionQueueConsumer>();
    }

    /// <summary>
    ///     Registers test case batching services that enable grouping and batch processing of related test
    ///     cases for cross-test-case analysis and comparison.
    /// </summary>
    private static void RegisterBatchingServices(IServiceCollection services)
    {
        services.AddSingleton<ITestCaseBatchingService, TestCaseBatchingService>();
        services.AddSingleton<IBatchTimeoutHandler, BatchTimeoutHandler>();
        services.AddSingleton<IQueueHealthCheck, QueueHealthCheck>();
    }

    /// <summary>
    ///     Registers queue processors that handle test completion messages from the queue. Processors are
    ///     registered conditionally based on configuration settings to enable flexible deployment and
    ///     feature enablement.
    /// </summary>
    private static void RegisterQueueProcessors(IServiceCollection services, QueueConfiguration configuration)
    {
        if (configuration.EnableFrameworkPublishing)
        {
            services.AddTransient<ITestCompletionQueueProcessor, FrameworkPublishingProcessor>();
        }

        if (configuration.EnableLoggingProcessor)
        {
            services.AddTransient<ITestCompletionQueueProcessor, LoggingQueueProcessor>();
        }

        if (configuration.EnableMethodComparison)
        {
            // Register unified formatter and its dependencies (mirrors the core lib registration so the
            // adapter resolves the adapter-flavoured implementations correctly).
            services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IImpactSummaryFormatter, Sailfish.Analysis.SailDiff.Formatting.ImpactSummaryFormatter>();
            services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IDetailedTableFormatter, Sailfish.Analysis.SailDiff.Formatting.DetailedTableFormatter>();
            services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IOutputContextAdapter, Sailfish.Analysis.SailDiff.Formatting.OutputContextAdapter>();
            services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IDistributionPlotFormatter, Sailfish.Analysis.SailDiff.Formatting.DistributionPlotFormatter>();
            services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.ISailDiffUnifiedFormatter, Sailfish.Analysis.SailDiff.Formatting.SailDiffUnifiedFormatter>();

            services.AddTransient<ITestCompletionQueueProcessor, MethodComparisonProcessor>();
            services.AddTransient<MethodComparisonBatchProcessor>();
        }
    }

    /// <summary>
    ///     Gets the queue configuration for service registration. Currently returns a default configuration
    ///     optimized for backward compatibility — the queue is enabled only when method comparison is on.
    /// </summary>
    private static QueueConfiguration GetQueueConfiguration()
    {
        // TODO: Integrate with the existing Sailfish settings system in future tasks. For now, this returns
        // a default configuration with the queue enabled only when method comparison is enabled.
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = 1000,
            PublishTimeoutMs = 5000,
            ProcessingTimeoutMs = 30000,
            BatchCompletionTimeoutMs = 60000,
            MaxRetryAttempts = 3,
            BaseRetryDelayMs = 1000,
            EnableBatchProcessing = true,
            MaxBatchSize = 50,
            EnableFrameworkPublishing = true,
            EnableLoggingProcessor = false,
            EnableComparisonAnalysis = false,
            EnableFallbackPublishing = true,
            LogLevel = LogLevel.Information,
            EnableMethodComparison = true,
            ComparisonDetectionStrategy = ComparisonDetectionStrategy.ByTestCaseCount,
            ComparisonTimeoutMs = 30000
        };

        config.IsEnabled = config.EnableMethodComparison;

        return config;
    }
}
