using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sailfish.Mediation;
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

        // Sailfish's in-house mediator. Registers IPublisher/ISender and scans this assembly for the
        // framework's notification/request handlers. Replaces the former MediatR dependency (and its
        // embedded community license key) — see Sailfish.Mediation.
        services.AddSailfishMediation();

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
        // Summary output writers — formerly the WriteTo{Console,MarkDown,Csv}Notification handlers, now
        // direct collaborators of ExecutionSummaryWriter (each had a single framework-owned handler).
        services.AddTransient<IConsoleSummaryWriter, ConsoleSummaryWriter>();
        services.AddTransient<IMarkdownSummaryWriter, MarkdownSummaryWriter>();
        services.AddTransient<ICsvSummaryWriter, CsvSummaryWriter>();
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
        services.AddTransient<IClassExecutionDispatcher, ClassExecutionDispatcher>();
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

        // Skipper AI analysis layer. Two seams a consumer can override:
        //   • ISkipperTransport — the common case: the model call only. Register with AddSkipperTransport<T>(),
        //     which also selects PromptDrivenSailfishAgent so the framework owns prompt-building and parsing.
        //   • ISailfishAgent — the advanced case: own the whole flow (prompt + transport + parse).
        // TryAdd keeps the no-op defaults for "nothing registered" (a hard-registered agent/transport wins
        // regardless of order), so when nobody wires Skipper the runner early-outs and it stays invisible.
        services.TryAddSingleton<ISailfishAgent, NoOpSailfishAgent>();
        services.TryAddSingleton<ISkipperTransport, NoOpSkipperTransport>();

        // The framework owns the intelligence: a rigorous default prompt (grounding preamble + composable body
        // sections + a sealed output-schema contract) and the parser that reads that schema back. Consumers extend
        // the prompt by registering additional ISkipperPromptSection implementations — they compose by Order.
        services.AddTransient<ISkipperPromptBuilder, DefaultSkipperPromptBuilder>();
        services.AddTransient<ISkipperResponseParser, DefaultSkipperResponseParser>();
        services.AddTransient<ISkipperPromptSection, ComparisonsPromptSection>();
        services.AddTransient<ISkipperPromptSection, ScalingPromptSection>();
        services.AddTransient<ISkipperPromptSection, EnvironmentPromptSection>();
        services.AddTransient<ISkipperPromptSection, ResultTablePromptSection>();

        services.AddTransient<IPerformanceNarrativeContextBuilder, PerformanceNarrativeContextBuilder>();
        services.AddTransient<ISkipperReviewWriter, SkipperReviewWriter>();
        services.AddTransient<ISkipperReportWriter, SkipperReportWriter>();
        services.AddTransient<ISkipperResponseCache, FileSkipperResponseCache>();
        services.AddTransient<ISkipperConsoleFormatter, SkipperConsoleFormatter>();
        services.AddTransient<ISkipperAnalysisRunner, SkipperAnalysisRunner>();

        return services;
    }
}
