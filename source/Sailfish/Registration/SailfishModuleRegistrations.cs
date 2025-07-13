using Autofac;
using MediatR;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Presentation.CsvAndJson;
using Sailfish.Presentation.Markdown;

namespace Sailfish.Registration;

internal class SailfishModuleRegistrations : IProvideAdditionalRegistrations
{
    private readonly IRunSettings runSettings;

    public SailfishModuleRegistrations(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(
            runSettings.DisableLogging
                ? new SilentLogger()
                : runSettings.CustomLogger ?? new DefaultLogger(runSettings.MinimumLogLevel)).As<ILogger>();
        builder.RegisterMediatR(MediatRConfigurationBuilder
            .Create(MediatrCommunityLicenseString, typeof(SailfishModuleRegistrations).Assembly)
            .Build());
        builder.RegisterAssemblyTypes(typeof(SailfishModuleRegistrations).Assembly)
            .AsClosedTypesOf(typeof(INotificationHandler<>))
            .AsImplementedInterfaces();
        builder.RegisterAssemblyTypes(typeof(SailfishModuleRegistrations).Assembly)
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces();

        builder.RegisterInstance(runSettings).As<IRunSettings>();
        builder.RegisterType<TestCaseCountPrinter>().As<ITestCaseCountPrinter>().SingleInstance();
        builder.RegisterType<SailfishExecutor>().AsSelf();
        builder.RegisterType<SailFishTestExecutor>().As<ISailFishTestExecutor>();
        builder.RegisterType<TestFilter>().As<ITestFilter>();
        builder.RegisterType<TestListValidator>().As<ITestListValidator>();
        builder.RegisterType<TestCollector>().As<ITestCollector>();
        builder.RegisterType<ParameterCombinator>().As<IParameterCombinator>();
        builder.RegisterType<PropertySetGenerator>().As<IPropertySetGenerator>();
        builder.RegisterType<TestInstanceContainerCreator>().As<ITestInstanceContainerCreator>();
        builder.RegisterType<TestCaseIterator>().As<ITestCaseIterator>();
        builder.RegisterType<StatisticsCompiler>().As<IStatisticsCompiler>();
        builder.RegisterType<ClassExecutionSummaryCompiler>().As<IClassExecutionSummaryCompiler>();
        builder.RegisterType<ExecutionSummaryWriter>().As<IExecutionSummaryWriter>();
        builder.RegisterType<MarkdownWriter>().As<IMarkdownWriter>();
        builder.RegisterType<ConsoleWriter>().As<IConsoleWriter>();
        builder.RegisterType<PerformanceRunResultFileWriter>().As<IPerformanceRunResultFileWriter>();
        builder.RegisterType<TrackingFileFinder>().As<ITrackingFileFinder>();
        builder.RegisterType<DefaultTrackingFileDirectoryReader>().As<ITrackingFileDirectoryReader>();
        builder.RegisterType<IterationVariableRetriever>().As<IIterationVariableRetriever>();
        builder.RegisterType<SailDiffResultMarkdownConverter>().As<ISailDiffResultMarkdownConverter>();
        builder.RegisterType<SailfishExecutionEngine>().As<ISailfishExecutionEngine>().InstancePerDependency();
        builder.RegisterType<MarkdownTableConverter>().As<IMarkdownTableConverter>().InstancePerDependency();
        builder.RegisterType<TrackingFileParser>().As<ITrackingFileParser>();
        builder.RegisterType<SailDiff>().As<ISailDiffInternal>().InstancePerDependency();
        builder.RegisterType<SailDiff>().As<ISailDiff>().InstancePerDependency();
        builder.RegisterType<ScaleFish>().As<IScaleFishInternal>().InstancePerDependency();
        builder.RegisterType<ScaleFish>().As<IScaleFish>().InstancePerDependency();
        builder.RegisterType<TrackingFileSerialization>().As<ITrackingFileSerialization>();
        builder.RegisterType<TypeActivator>().As<ITypeActivator>();
        builder.RegisterType<StatisticalTestComputer>().As<IStatisticalTestComputer>();
        builder.RegisterType<TestPreprocessor>().As<ITestPreprocessor>();
        builder.RegisterType<StatisticalTestExecutor>().As<IStatisticalTestExecutor>();
        builder.RegisterType<PerformanceRunResultAggregator>().As<IPerformanceRunResultAggregator>();
        builder.RegisterType<ComplexityComputer>().As<IComplexityComputer>();
        builder.RegisterType<ComplexityEstimator>().As<IComplexityEstimator>();
        builder.RegisterType<SailfishOutlierDetector>().As<ISailfishOutlierDetector>();
        builder.RegisterType<Test>().As<ITTest>();
        builder.RegisterType<MannWhitneyWilcoxonTest>().As<IMannWhitneyWilcoxonTest>();
        builder.RegisterType<TwoSampleWilcoxonSignedRankTest>().As<ITwoSampleWilcoxonSignedRankTest>();
        builder.RegisterType<KolmogorovSmirnovTest>().As<IKolmogorovSmirnovTest>();
        builder.RegisterType<ScalefishObservationCompiler>().As<IScalefishObservationCompiler>();
        builder.RegisterType<SailDiffConsoleWindowMessageFormatter>().As<ISailDiffConsoleWindowMessageFormatter>();
        builder.RegisterType<MethodComparisonCoordinator>().As<IMethodComparisonCoordinator>().SingleInstance();
        builder.RegisterType<MethodComparisonPresenter>().As<INotificationHandler<MethodComparisonCompletedNotification>>().SingleInstance();
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