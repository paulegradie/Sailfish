using Autofac;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Registration;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Handlers.TestCaseEvents;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Sailfish.TestAdapter.Queue.Processors;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;
    using Sailfish.Diagnostics.Environment;
    using Sailfish.TestAdapter.Execution.EnvironmentHealth;


namespace Sailfish.TestAdapter.Registrations;

internal class TestAdapterRegistrations : IProvideAdditionalRegistrations
{
    private readonly IFrameworkHandle? _frameworkHandle;

    public TestAdapterRegistrations(IFrameworkHandle? frameworkHandle)
    {
        _frameworkHandle = frameworkHandle;
    }

    public void Load(ContainerBuilder builder)
    {
        if (_frameworkHandle is not null) builder.RegisterInstance(_frameworkHandle).As<IFrameworkHandle>();

        builder.RegisterType<TestAdapterExecutionProgram>().As<ITestAdapterExecutionProgram>();
        builder.RegisterType<TestAdapterExecutionEngine>().As<ITestAdapterExecutionEngine>();
        builder.RegisterType<AdapterSailDiff>().As<ISailDiffInternal>().InstancePerDependency();
        builder.RegisterType<AdapterSailDiff>().As<IAdapterSailDiff>();
        builder.RegisterType<AdapterScaleFish>().As<IScaleFishInternal>();
        builder.RegisterType<AdapterScaleFish>().As<IAdapterScaleFish>();
        builder.RegisterType<TestCaseCountPrinter>().As<ITestCaseCountPrinter>().SingleInstance();
        builder.RegisterType<TestFrameworkWriter>().As<ITestFrameworkWriter>().SingleInstance();
        builder.RegisterType<EnvironmentHealthChecker>().As<IEnvironmentHealthChecker>().SingleInstance();
        builder.RegisterType<EnvironmentHealthCheckRunner>().AsSelf().SingleInstance();

        builder.RegisterType<SailDiffTestOutputWindowMessageFormatter>().As<ISailDiffTestOutputWindowMessageFormatter>();
        builder.RegisterType<SailfishConsoleWindowFormatter>().As<ISailfishConsoleWindowFormatter>();

        builder.RegisterType<TestCaseStartedNotificationHandler>().As<INotificationHandler<TestCaseStartedNotification>>();
        builder.RegisterType<TestCaseCompletedNotificationHandler>().As<INotificationHandler<TestCaseCompletedNotification>>();
        builder.RegisterType<TestCaseDisabledNotificationHandler>().As<INotificationHandler<TestCaseDisabledNotification>>();
        builder.RegisterType<TestCaseExceptionNotificationHandler>().As<INotificationHandler<TestCaseExceptionNotification>>();

        builder.RegisterType<FrameworkTestCaseEndNotificationHandler>().As<INotificationHandler<FrameworkTestCaseEndNotification>>();

        // Register queue services conditionally based on configuration
        RegisterQueueServices(builder);
    }

    /// <summary>
    /// Registers queue services in the dependency injection container based on queue configuration.
    /// This method implements conditional registration to ensure queue services are only registered
    /// when the queue system is enabled, maintaining backward compatibility with existing functionality.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register services with.</param>
    /// <remarks>
    /// This method registers all components of the intercepting queue architecture including:
    /// - Core queue services (queue, publisher, factory, manager, consumer)
    /// - Batching services for cross-test-case analysis
    /// - Queue processors for framework publishing and logging
    /// - Configuration services for queue settings management
    ///
    /// Services are registered with appropriate lifetimes:
    /// - Singleton: Queue, manager, consumer, batching service, factory, configuration
    /// - Transient: Publisher, processors
    ///
    /// The registration is conditional based on the queue configuration's IsEnabled property.
    /// When disabled, no queue services are registered, ensuring the test adapter falls back
    /// to direct framework publishing for backward compatibility.
    ///
    /// Integration with existing Sailfish DI patterns:
    /// - Uses standard Autofac registration patterns consistent with existing services
    /// - Maintains service lifetime conventions used throughout the test adapter
    /// - Supports configuration-driven service enablement for flexible deployment
    /// - Ensures proper dependency resolution for all queue components
    /// </remarks>
    private void RegisterQueueServices(ContainerBuilder builder)
    {
        var queueConfiguration = GetQueueConfiguration();

        // Always register the configuration for potential use by other services
        builder.RegisterInstance(queueConfiguration).As<QueueConfiguration>().SingleInstance();

        // Only register queue services if the queue system is enabled
        if (!queueConfiguration.IsEnabled)
        {
            return;
        }

        RegisterCoreQueueServices(builder);
        RegisterBatchingServices(builder);
        RegisterQueueProcessors(builder, queueConfiguration);
    }

    /// <summary>
    /// Registers core queue infrastructure services including the queue, publisher, factory,
    /// manager, and consumer services. These services form the foundation of the intercepting
    /// queue architecture that enables asynchronous processing and batch analysis.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register services with.</param>
    /// <remarks>
    /// Core services registered:
    /// - ITestCompletionQueue → InMemoryTestCompletionQueue (Singleton)
    /// - ITestCompletionQueuePublisher → TestCompletionQueuePublisher (Transient)
    /// - ITestCompletionQueueFactory → TestCompletionQueueFactory (Singleton)
    /// - TestCompletionQueueManager (Singleton)
    /// - TestCompletionQueueConsumer (Singleton)
    ///
    /// Service lifetimes are chosen based on usage patterns:
    /// - Singleton services manage state and lifecycle across test execution
    /// - Transient services are lightweight and stateless
    ///
    /// These services work together to provide the intercepting queue functionality
    /// where test completion messages are queued for processing before being
    /// reported to the VS Test Platform.
    /// </remarks>
    private static void RegisterCoreQueueServices(ContainerBuilder builder)
    {
        // Core queue implementation - singleton to maintain state during test execution
        // Use factory registration to inject the MaxQueueCapacity configuration parameter
        builder.Register(context =>
        {
            var configuration = context.Resolve<QueueConfiguration>();
            return new InMemoryTestCompletionQueue(configuration.MaxQueueCapacity);
        })
        .As<ITestCompletionQueue>()
        .SingleInstance();

        // Queue publisher - transient as it's a lightweight service
        builder.RegisterType<TestCompletionQueuePublisher>()
            .As<ITestCompletionQueuePublisher>()
            .InstancePerDependency();



        // Queue manager - singleton to coordinate queue lifecycle
        // Use factory registration to inject processors from container
        builder.Register(context =>
        {
            var queue = context.Resolve<ITestCompletionQueue>();
            var processors = context.Resolve<ITestCompletionQueueProcessor[]>();
            var logger = context.Resolve<ILogger>();
            // Note: Health check will be resolved later to avoid circular dependency
            return new TestCompletionQueueManager(queue, processors, logger);
        })
        .AsSelf()
        .SingleInstance();

        // Queue consumer - singleton to maintain processing state
        builder.RegisterType<TestCompletionQueueConsumer>()
            .AsSelf()
            .SingleInstance();
    }

    /// <summary>
    /// Registers test case batching services that enable grouping and batch processing
    /// of related test cases for cross-test-case analysis and comparison.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register services with.</param>
    /// <remarks>
    /// Batching services registered:
    /// - ITestCaseBatchingService → TestCaseBatchingService (Singleton)
    ///
    /// The batching service is registered as a singleton to maintain batch state
    /// and grouping information across the entire test execution. This enables
    /// cross-test-case analysis by collecting related test cases into batches
    /// that can be processed together by queue processors.
    ///
    /// The batching service supports multiple batching strategies including:
    /// - ByTestClass: Group test cases by their containing test class
    /// - ByComparisonAttribute: Group test cases by comparison attributes
    /// - ByCustomCriteria: Group test cases using custom criteria
    /// - ByExecutionContext: Group test cases by execution context
    /// - ByPerformanceProfile: Group test cases by performance characteristics
    ///
    /// This service integrates with the queue system to enable batch completion
    /// detection and timeout handling for incomplete batches.
    /// </remarks>
    private static void RegisterBatchingServices(ContainerBuilder builder)
    {
        // Test case batching service - singleton to maintain batch state across test execution
        builder.RegisterType<TestCaseBatchingService>()
            .As<ITestCaseBatchingService>()
            .SingleInstance();

        // Batch timeout handler - singleton to monitor and handle batch timeouts
        builder.RegisterType<BatchTimeoutHandler>()
            .As<IBatchTimeoutHandler>()
            .SingleInstance();

        // Queue health check - singleton to monitor queue health and performance
        builder.RegisterType<QueueHealthCheck>()
            .As<IQueueHealthCheck>()
            .SingleInstance();


    }

    /// <summary>
    /// Registers queue processors that handle test completion messages from the queue.
    /// Processors are registered conditionally based on configuration settings to enable
    /// flexible deployment and feature enablement.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register services with.</param>
    /// <param name="configuration">The queue configuration containing processor enablement settings.</param>
    /// <remarks>
    /// Queue processors registered conditionally:
    /// - FrameworkPublishingProcessor: Always registered when queue is enabled (required for VS Test Platform integration)
    /// - LoggingQueueProcessor: Registered when EnableLoggingProcessor is true
    ///
    /// All processors are registered as transient services since they are lightweight
    /// and stateless. Processors implement the ITestCompletionQueueProcessor interface
    /// and extend the TestCompletionQueueProcessorBase abstract class.
    ///
    /// The FrameworkPublishingProcessor is critical for the intercepting queue architecture
    /// as it's responsible for publishing FrameworkTestCaseEndNotification messages to
    /// the VS Test Platform. Without this processor, test results would not appear in
    /// test explorers.
    ///
    /// Additional processors can be registered here as they are implemented in future
    /// phases of the queue migration project, including comparison processors, analysis
    /// processors, and custom processors for specific testing scenarios.
    ///
    /// Processor registration follows the existing Sailfish DI patterns and integrates
    /// with the MediatR notification system for framework publishing.
    /// </remarks>
    private static void RegisterQueueProcessors(ContainerBuilder builder, QueueConfiguration configuration)
    {
        // Framework publishing processor - always register when queue is enabled
        // This processor is critical for publishing test results to VS Test Platform
        if (configuration.EnableFrameworkPublishing)
        {
            builder.RegisterType<FrameworkPublishingProcessor>()
                .As<ITestCompletionQueueProcessor>()
                .InstancePerDependency();
        }

        // Logging processor - register conditionally based on configuration
        if (configuration.EnableLoggingProcessor)
        {
            builder.RegisterType<LoggingQueueProcessor>()
                .As<ITestCompletionQueueProcessor>()
                .InstancePerDependency();
        }

        // Method comparison processors - register conditionally based on configuration
        if (configuration.EnableMethodComparison)
        {
            // Register unified formatter and its dependencies
            builder.RegisterType<Sailfish.Analysis.SailDiff.Formatting.ImpactSummaryFormatter>()
                .As<Sailfish.Analysis.SailDiff.Formatting.IImpactSummaryFormatter>()
                .InstancePerDependency();

            builder.RegisterType<Sailfish.Analysis.SailDiff.Formatting.DetailedTableFormatter>()
                .As<Sailfish.Analysis.SailDiff.Formatting.IDetailedTableFormatter>()
                .InstancePerDependency();

            builder.RegisterType<Sailfish.Analysis.SailDiff.Formatting.OutputContextAdapter>()
                .As<Sailfish.Analysis.SailDiff.Formatting.IOutputContextAdapter>()
                .InstancePerDependency();

            builder.RegisterType<Sailfish.Analysis.SailDiff.Formatting.SailDiffUnifiedFormatter>()
                .As<Sailfish.Analysis.SailDiff.Formatting.ISailDiffUnifiedFormatter>()
                .InstancePerDependency();

            builder.RegisterType<MethodComparisonProcessor>()
                .As<ITestCompletionQueueProcessor>()
                .InstancePerDependency();

            builder.RegisterType<MethodComparisonBatchProcessor>()
                .AsSelf()
                .InstancePerDependency();
        }

        // Additional processors will be registered here as they are implemented
        // in future phases of the queue migration project
    }

    /// <summary>
    /// Gets the queue configuration for service registration. This method provides
    /// the configuration settings that determine which queue services should be
    /// registered and how they should be configured.
    /// </summary>
    /// <returns>A <see cref="QueueConfiguration"/> instance with appropriate settings for the current environment.</returns>
    /// <remarks>
    /// This method currently returns a default configuration optimized for backward compatibility.
    /// The queue system is disabled by default (IsEnabled = false) to ensure existing
    /// functionality continues to work unchanged.
    ///
    /// Future enhancements will integrate this method with the existing Sailfish settings
    /// system to load configuration from:
    /// - .sailfish.json configuration files
    /// - Environment variables
    /// - Command-line arguments
    /// - Programmatic configuration
    ///
    /// The default configuration provides:
    /// - Queue disabled for backward compatibility
    /// - Conservative timeout and capacity settings
    /// - Framework publishing enabled (when queue is enabled)
    /// - Logging and comparison processors disabled by default
    /// - Fallback publishing enabled for reliability
    ///
    /// Configuration loading strategy:
    /// 1. Load from .sailfish.json if available
    /// 2. Apply environment variable overrides
    /// 3. Fall back to safe defaults if configuration is missing or invalid
    /// 4. Validate configuration before returning
    ///
    /// This approach ensures the queue system can be gradually adopted without
    /// breaking existing test execution workflows.
    /// </remarks>
    private static QueueConfiguration GetQueueConfiguration()
    {
        // TODO: Integrate with existing Sailfish settings system in future tasks
        // For now, return default configuration with queue disabled for backward compatibility
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

        // Enable queue system when method comparison is enabled
        // This ensures the comparison feature works while maintaining backward compatibility
        config.IsEnabled = config.EnableMethodComparison;

        return config;
    }
}