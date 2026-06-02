using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sailfish.Analysis;
using Sailfish.Analysis.Ai;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.PermutationTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Presentation.CsvAndJson;
using Sailfish.Presentation.Markdown;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;

namespace Sailfish.Registration;

/// <summary>
///     Core Sailfish service registrations. Adds every framework service required to run benchmarks.
///     This is invoked by <see cref="AssemblyRegistrationExtensionMethods.AddSailfish"/> — consumers should not
///     call into it directly.
/// </summary>
internal static class SailfishModuleRegistrations
{
    public const string FixedIterationStrategyKey = "Fixed";
    public const string AdaptiveIterationStrategyKey = "Adaptive";

    public static IServiceCollection AddSailfishCore(this IServiceCollection services, IRunSettings runSettings)
    {
        // Logger — instance-valued, depends on the runSettings configuration.
        ILogger logger = runSettings.DisableLogging
            ? new SilentLogger()
            : runSettings.CustomLogger ?? new DefaultLogger(runSettings.MinimumLogLevel);
        services.AddSingleton(logger);

        // MediatR 14+ on the MS DI path calls ILoggerFactory during its license check, so we ensure logging
        // infrastructure exists. AddLogging is idempotent — if the caller has already registered logging,
        // this is a no-op.
        services.AddLogging();

        // MediatR — scan this assembly for handlers using the native IServiceCollection extension.
        // The community license key is required by MediatR 14+.
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = MediatrCommunityLicenseString;
            cfg.RegisterServicesFromAssemblyContaining(typeof(SailfishModuleRegistrations));
        });

        // Run settings — singleton, passed in by caller.
        services.AddSingleton(runSettings);

        services.AddSingleton<ITestCaseCountPrinter, TestCaseCountPrinter>();
        services.AddTransient<SailfishExecutor>();
        services.AddTransient<ISailFishTestExecutor, SailFishTestExecutor>();
        services.AddTransient<ITestFilter, TestFilter>();
        services.AddTransient<ITestListValidator, TestListValidator>();
        services.AddTransient<ITestCollector, TestCollector>();
        services.AddTransient<IParameterCombinator, ParameterCombinator>();
        services.AddTransient<IPropertySetGenerator, PropertySetGenerator>();
        services.AddTransient<ITestInstanceContainerCreator, TestInstanceContainerCreator>();

        // Statistical convergence detector — singleton, holds no per-run state but is expensive to allocate.
        services.AddSingleton<IStatisticalConvergenceDetector, StatisticalConvergenceDetector>();

        // Iteration strategies registered as keyed singletons; TestCaseIterator resolves both by key.
        services.AddKeyedSingleton<IIterationStrategy, FixedIterationStrategy>(FixedIterationStrategyKey);
        services.AddKeyedSingleton<IIterationStrategy, AdaptiveIterationStrategy>(AdaptiveIterationStrategyKey);

        services.AddSingleton<ITestCaseIterator>(sp => new TestCaseIterator(
            sp.GetRequiredService<IRunSettings>(),
            sp.GetRequiredService<ILogger>(),
            sp.GetRequiredKeyedService<IIterationStrategy>(FixedIterationStrategyKey),
            sp.GetRequiredKeyedService<IIterationStrategy>(AdaptiveIterationStrategyKey),
            sp.GetService<IStatisticalTestExecutor>()));

        services.AddTransient<IStatisticsCompiler, StatisticsCompiler>();
        services.AddTransient<IClassExecutionSummaryCompiler, ClassExecutionSummaryCompiler>();
        services.AddTransient<IExecutionSummaryWriter, ExecutionSummaryWriter>();
        services.AddTransient<IMarkdownWriter, MarkdownWriter>();
        services.AddTransient<IConsoleWriter, ConsoleWriter>();
        services.AddTransient<IPerformanceRunResultFileWriter, PerformanceRunResultFileWriter>();
        services.AddTransient<ITrackingFileFinder, TrackingFileFinder>();
        services.AddTransient<ITrackingFileDirectoryReader, DefaultTrackingFileDirectoryReader>();
        services.AddTransient<IIterationVariableRetriever, IterationVariableRetriever>();

        // Unified formatter components for legacy SailDiff (Transient — instance per dependency).
        services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IImpactSummaryFormatter, Sailfish.Analysis.SailDiff.Formatting.ImpactSummaryFormatter>();
        services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IDetailedTableFormatter, Sailfish.Analysis.SailDiff.Formatting.DetailedTableFormatter>();
        services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IOutputContextAdapter, Sailfish.Analysis.SailDiff.Formatting.OutputContextAdapter>();
        services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.IDistributionPlotFormatter, Sailfish.Analysis.SailDiff.Formatting.DistributionPlotFormatter>();
        services.AddTransient<Sailfish.Analysis.SailDiff.Formatting.ISailDiffUnifiedFormatter, Sailfish.Analysis.SailDiff.Formatting.SailDiffUnifiedFormatter>();

        services.AddTransient<ISailDiffResultMarkdownConverter, SailDiffResultMarkdownConverter>();
        services.AddTransient<ISailfishExecutionEngine, SailfishExecutionEngine>();
        services.AddSingleton<IReproducibilityManifestProvider, ReproducibilityManifestProvider>();
        services.AddSingleton<IEnvironmentHealthReportProvider, EnvironmentHealthReportProvider>();

        // Timer calibration service and provider — session-scoped singletons.
        services.AddSingleton<ITimerCalibrationService, TimerCalibrationService>();
        services.AddSingleton<ITimerCalibrationResultProvider, TimerCalibrationResultProvider>();

        services.AddTransient<IMarkdownTableConverter, MarkdownTableConverter>();
        services.AddTransient<ITrackingFileParser, TrackingFileParser>();

        // SailDiff has two interface views over a single implementation; register both as transient.
        services.AddTransient<ISailDiffInternal, SailDiff>();
        services.AddTransient<ISailDiff, SailDiff>();
        services.AddTransient<IScaleFishInternal, ScaleFish>();
        services.AddTransient<IScaleFish, ScaleFish>();

        services.AddTransient<ITrackingFileSerialization, TrackingFileSerialization>();
        services.AddTransient<ITypeActivator, TypeActivator>();
        services.AddTransient<IStatisticalTestComputer, StatisticalTestComputer>();
        services.AddTransient<ITestPreprocessor, TestPreprocessor>();
        services.AddTransient<IStatisticalTestExecutor, StatisticalTestExecutor>();
        services.AddTransient<IPerformanceRunResultAggregator, PerformanceRunResultAggregator>();
        services.AddTransient<IComplexityComputer, ComplexityComputer>();
        services.AddTransient<IComplexityEstimator, ComplexityEstimator>();
        services.AddTransient<ISailfishOutlierDetector, SailfishOutlierDetector>();
        services.AddTransient<ITTest, Test>();
        services.AddTransient<IMannWhitneyWilcoxonTest, MannWhitneyWilcoxonTest>();
        services.AddTransient<ITwoSampleWilcoxonSignedRankTest, TwoSampleWilcoxonSignedRankTest>();
        services.AddTransient<IKolmogorovSmirnovTest, KolmogorovSmirnovTest>();
        // SailDiff Tier 3 permutation test (added in #249, merged from main).
        services.AddTransient<IPermutationTest, PermutationTest>();
        services.AddTransient<IScalefishObservationCompiler, ScalefishObservationCompiler>();
        services.AddTransient<ISailDiffConsoleWindowMessageFormatter, SailDiffConsoleWindowMessageFormatter>();

        // Skipper AI analysis layer. The agent is the only seam a consumer overrides; TryAdd means a
        // user-registered ISailfishAgent (from IRegisterSailfishServices) wins regardless of registration order.
        services.TryAddSingleton<ISailfishAgent, NoOpSailfishAgent>();
        services.AddTransient<IPerformanceNarrativeContextBuilder, PerformanceNarrativeContextBuilder>();
        services.AddTransient<ISkipperReviewWriter, SkipperReviewWriter>();
        services.AddTransient<ISkipperReportWriter, SkipperReportWriter>();
        services.AddTransient<ISkipperResponseCache, FileSkipperResponseCache>();
        services.AddTransient<ISkipperConsoleFormatter, SkipperConsoleFormatter>();
        services.AddTransient<ISkipperAnalysisRunner, SkipperAnalysisRunner>();

        return services;
    }

    private const string MediatrCommunityLicenseString =
        "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2" +
        "IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhd" +
        "WQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzgzOTAwODAwIiwiaWF0IjoiMTc1MjQwOTMzMCIsImFjY291bnR" +
        "faWQiOiIwMTk4MDNiOGZmM2I3Zjg1OWRhYTQ3ZDAxNjRmNzRhMCIsImN1c3RvbWVyX2lkIjoiY3RtXzAxazAxdnJuNnlwZ" +
        "XZ3MW16bm04dDN3Z3IwIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.wlaAwDaMwjOph1gw7UH1" +
        "tesVDWym25fVn1jA_xJ3yTinTIoiedDxy6STARKqWIw97d44RB2-WXT4E_bNKTLxheAEeUiycH1RzCRvfl8n5qsRbHbu8J" +
        "PyhqdPUBP7uNDWPU60YzcsCQeeL607w3G4qTD9jUN8eXz_nMqo4MJDwdwsyUOppfuRKhHNz8CGvYGdKOtYSQFsQa5JoF9W" +
        "sS2hvUkGnpqCzWZRCdCDh22TUzuXJklV8iYYvOrNKZ-gbEdTJligEhcHAI-sopdK0J-SzGOxJyiX4nBe3A1OduLG-S58QZ" +
        "8g6oOIdWuTWgyhTPYE0oqsBZ5nEqsS42rnEF2cbw";
}
