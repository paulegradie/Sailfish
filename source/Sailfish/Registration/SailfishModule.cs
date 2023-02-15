﻿using Autofac;
using Microsoft.Extensions.Configuration;
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
        var configuration = new ConfigurationBuilder().AddJsonFile("sailfish.logging.json", true).Build();

        base.Load(builder);
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
        // builder.RegisterType<LifetimeScopeTypeResolver>().As<ITypeResolver>();
        builder.RegisterType<TestCaseIterator>().As<ITestCaseIterator>();
        builder.RegisterType<StatisticsCompiler>().As<IStatisticsCompiler>();
        builder.RegisterType<ExecutionSummaryCompiler>().As<IExecutionSummaryCompiler>();
        builder.RegisterType<TestResultPresenter>().As<ITestResultPresenter>();
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
        builder.RegisterType<TestResultAnalyzer>().As<ITestResultAnalyzer>();
        builder.RegisterType<SailfishExecutionEngine>().As<ISailfishExecutionEngine>();
        builder.RegisterType<MarkdownTableConverter>().As<IMarkdownTableConverter>().InstancePerDependency();
    }
}