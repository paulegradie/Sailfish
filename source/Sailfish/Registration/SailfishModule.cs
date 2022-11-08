using Autofac;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Presentation.Csv;
using Sailfish.Presentation.Markdown;
using Sailfish.Statistics;
using Sailfish.Statistics.Tests.MWWilcoxonTest;
using Sailfish.Statistics.Tests.TTest;
using Serilog;

namespace Sailfish.Registration;

public class SailfishModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.Register<ILogger>(
            (c, p) => new LoggerConfiguration()
                .CreateLogger()).SingleInstance();

        builder.RegisterType<SailfishExecutor>().AsSelf();
        builder.RegisterType<SailFishTestExecutor>().As<ISailFishTestExecutor>();
        builder.RegisterType<TestFilter>().As<ITestFilter>();
        builder.RegisterType<TestListValidator>().As<ITestListValidator>();
        builder.RegisterType<TestCollector>().As<ITestCollector>();
        builder.RegisterType<ParameterCombinator>().As<IParameterCombinator>();
        builder.RegisterType<ParameterGridCreator>().As<IParameterGridCreator>();
        builder.RegisterType<TestInstanceContainerCreator>().As<ITestInstanceContainerCreator>();
        builder.RegisterType<TypeResolver>().As<ITypeResolver>();
        builder.RegisterType<TestCaseIterator>().As<ITestCaseIterator>();
        builder.RegisterType<StatisticsCompiler>().As<IStatisticsCompiler>();
        builder.RegisterType<ExecutionSummaryCompiler>().As<IExecutionSummaryCompiler>();
        builder.RegisterType<TestResultPresenter>().As<ITestResultPresenter>();
        builder.RegisterType<PresentationStringConstructor>().As<IPresentationStringConstructor>().InstancePerDependency();
        builder.RegisterType<FileIo>().As<IFileIo>();
        builder.RegisterType<MarkdownWriter>().As<IMarkdownWriter>();
        builder.RegisterType<ConsoleWriter>().As<IConsoleWriter>();
        builder.RegisterType<PerformanceCsvWriter>().As<IPerformanceCsvWriter>();
        builder.RegisterType<TTest>().As<ITTest>();
        builder.RegisterType<MannWhitneyWilcoxonTest>().As<IMannWhitneyWilcoxonTest>();
        builder.RegisterType<TrackingFileFinder>().As<ITrackingFileFinder>();
        builder.RegisterType<PerformanceCsvTrackingWriter>().As<IPerformanceCsvTrackingWriter>();
        builder.RegisterType<IterationVariableRetriever>().As<IIterationVariableRetriever>();
        builder.RegisterType<TestResultsCsvWriter>().As<ITestResultsCsvWriter>();
        builder.RegisterType<TestResultTableContentFormatter>().As<ITestResultTableContentFormatter>();
    }
}