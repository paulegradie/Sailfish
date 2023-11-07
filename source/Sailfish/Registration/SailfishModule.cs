using Autofac;
using MediatR;
using Microsoft.Extensions.Configuration;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Presentation.CsvAndJson;
using Sailfish.Presentation.Markdown;
using Sailfish.Statistics;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Serilog;

namespace Sailfish.Registration;

public class SailfishModule : Module
{
    private readonly IRunSettings runSettings;

    public SailfishModule(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        var configuration = new ConfigurationBuilder().AddJsonFile("sailfish.logging.json", true).Build();

        builder
            .RegisterType<Mediator>()
            .As<IMediator>()
            .InstancePerLifetimeScope();
        builder.Register<ServiceFactory>(
            context =>
            {
                var c = context.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });
        builder.RegisterAssemblyTypes(typeof(SailfishExecutor).Assembly)
            .Where(x => x != typeof(ISailfishDependency))
            .AsImplementedInterfaces(); // via assembly scan

        builder.RegisterInstance(runSettings).As<IRunSettings>();

        builder.Register<ILogger>(
            (c, p) =>
                new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .WriteTo.Console()
                    .MinimumLevel.Verbose()
                    .CreateLogger()).SingleInstance();

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
        builder.RegisterType<FileIo>().As<IFileIo>();
        builder.RegisterType<MarkdownWriter>().As<IMarkdownWriter>();
        builder.RegisterType<ConsoleWriter>().As<IConsoleWriter>();
        builder.RegisterType<PerformanceRunResultFileWriter>().As<IPerformanceRunResultFileWriter>();
        builder.RegisterType<TrackingFileFinder>().As<ITrackingFileFinder>();
        builder.RegisterType<IterationVariableRetriever>().As<IIterationVariableRetriever>();
        builder.RegisterType<SailDiffResultMarkdownConverter>().As<ISailDiffResultMarkdownConverter>();
        builder.RegisterType<SailfishExecutionEngine>().As<ISailfishExecutionEngine>();
        builder.RegisterType<MarkdownTableConverter>().As<IMarkdownTableConverter>().InstancePerDependency();
        builder.RegisterType<TrackingFileParser>().As<ITrackingFileParser>();
        builder.RegisterType<SailDiff>().As<ISailDiffInternal>().InstancePerDependency();
        builder.RegisterType<SailDiff>().As<ISailDiff>().InstancePerDependency();
        builder.RegisterType<ScaleFish>().As<IScaleFishInternal>().InstancePerDependency();
        builder.RegisterType<ScaleFish>().As<IScaleFish>().InstancePerDependency();
        builder.RegisterType<TrackingFileSerialization>().As<ITrackingFileSerialization>();
        builder.RegisterType<TypeActivator>().As<ITypeActivator>();
        builder.RegisterType<TestComputer>().As<ITestComputer>();
        builder.RegisterType<TestPreprocessor>().As<ITestPreprocessor>();
        builder.RegisterType<StatisticalTestExecutor>().As<IStatisticalTestExecutor>();
        builder.RegisterType<PerformanceRunResultAggregator>().As<IPerformanceRunResultAggregator>();
        builder.RegisterType<ComplexityComputer>().As<IComplexityComputer>();
        builder.RegisterType<ComplexityEstimator>().As<IComplexityEstimator>();
        builder.RegisterType<SailfishOutlierDetector>().As<ISailfishOutlierDetector>();
        builder.RegisterType<TTestSailfish>().As<ITTestSailfish>();
        builder.RegisterType<MannWhitneyWilcoxonTestSailfish>().As<IMannWhitneyWilcoxonTestSailfish>();
        builder.RegisterType<TwoSampleWilcoxonSignedRankTestSailfish>().As<ITwoSampleWilcoxonSignedRankTestSailfish>();
        builder.RegisterType<KolmogorovSmirnovTestSailfish>().As<IKolmogorovSmirnovTestSailfish>();
        builder.RegisterType<ScalefishObservationCompiler>().As<IScalefishObservationCompiler>();
    }
}