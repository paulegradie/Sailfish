using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Diagnostics.Environment;
using Sailfish.Logging;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Execution.Aggregation;
using Sailfish.TestAdapter.Execution.EnvironmentHealth;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Handlers.TestCaseEvents;
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

        RegisterComparisonAggregation(services);

        return services;
    }

    /// <summary>
    ///     Registers the test-completion aggregator and the cross-method comparison engine it drives. The
    ///     aggregator buffers the members of a comparison group until the group is complete (by known count),
    ///     then runs <see cref="MethodComparisonBatchProcessor" /> once — replacing the former in-memory queue
    ///     subsystem (queue/publisher/consumer/manager/batching/timeout/health-check) with a single synchronous
    ///     component. Additional <see cref="ITestCompletionSink" /> implementations registered here would be
    ///     observed for every completion (the extension seam the queue's chained processors used to provide).
    /// </summary>
    private static void RegisterComparisonAggregation(IServiceCollection services)
    {
        // Unified SailDiff formatter chain the comparison engine depends on (mirrors the core-lib registration
        // so the adapter resolves the adapter-flavoured implementations).
        services.AddTransient<IImpactSummaryFormatter, ImpactSummaryFormatter>();
        services.AddTransient<IDetailedTableFormatter, DetailedTableFormatter>();
        services.AddTransient<IOutputContextAdapter, OutputContextAdapter>();
        services.AddTransient<IDistributionPlotFormatter, DistributionPlotFormatter>();
        services.AddTransient<ISailDiffUnifiedFormatter, SailDiffUnifiedFormatter>();

        services.AddTransient<MethodComparisonBatchProcessor>();

        // Singleton: holds per-run comparison-group buffers across the whole test run.
        services.AddSingleton<TestCompletionAggregator>();
    }
}
