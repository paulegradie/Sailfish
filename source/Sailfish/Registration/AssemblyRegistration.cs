using System;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Presentation.Csv;
using Sailfish.Presentation.Markdown;
using Sailfish.Presentation.TTest;
using Sailfish.Statistics;
using Sailfish.Statistics.StatisticalAnalysis;
using Sailfish.Utils;
using Serilog;

namespace Sailfish.Registration
{
    public static class AssemblyRegistrationExtensionMethods
    {
        public static void RegisterSailfishTypes(this ContainerBuilder builder)
        {
            builder.RegisterModule(new ExecutorModule());
        }

        public static void RegisterPerformanceTypes(this ContainerBuilder builder, params Type[] sourceTypes)
        {
            var testCollector = new TestCollector();
            var allPerfTypes = testCollector.CollectTestTypes(sourceTypes);
            builder.RegisterTypes(allPerfTypes);
        }

        public static void RegisterPerformanceTypes(this IServiceCollection serviceCollection)
        {
            throw new NotImplementedException();
        }

        public static void RegisterSailfishTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ILogger>(
                c =>
                {
                    return new LoggerConfiguration()
                        .CreateLogger();
                });

            serviceCollection.AddTransient<SailfishExecutor>();
            serviceCollection.AddTransient<ISailTestExecutor, SailTestExecutor>();
            serviceCollection.AddTransient<ITestFilter, TestFilter>();
            serviceCollection.AddTransient<ITestListValidator, TestListValidator>();
            serviceCollection.AddTransient<ITestCollector, TestCollector>();
            serviceCollection.AddTransient<IParameterCombinator, ParameterCombinator>();
            serviceCollection.AddTransient<IParameterGridCreator, ParameterGridCreator>();
            serviceCollection.AddTransient<ITestInstanceContainerCreator, TestInstanceContainerCreator>();
            serviceCollection.AddTransient<ITypeResolver, TypeResolver>();
            serviceCollection.AddTransient<ITestCaseIterator, TestCaseIterator>();
            serviceCollection.AddTransient<IStatisticsCompiler, StatisticsCompiler>();
            serviceCollection.AddTransient<ITestResultCompiler, TestResultCompiler>();
            serviceCollection.AddTransient<ITestResultPresenter, TestResultPresenter>();
            serviceCollection.AddTransient<IFileIo, FileIo>();
            serviceCollection.AddTransient<IPresentationStringConstructor, PresentationStringConstructor>();
            serviceCollection.AddTransient<IConsoleWriter, ConsoleWriter>();
            serviceCollection.AddTransient<IPerformanceCsvWriter, PerformanceCsvWriter>();
            serviceCollection.AddTransient<IMarkdownWriter, MarkdownWriter>();
            serviceCollection.AddTransient<ITwoTailedTTestWriter, TwoTailedTTestWriter>();
            serviceCollection.AddTransient<ITrackingFileFinder, TrackingFileFinder>();
            serviceCollection.AddTransient<IPerformanceCsvTrackingWriter, PerformanceCsvTrackingWriter>();
            serviceCollection.AddTransient<IIterationVariableRetriever, IterationVariableRetriever>();
        }
    }

    public class ExecutorModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.Register<ILogger>(
                (c, p) =>
                {
                    return new LoggerConfiguration()
                        .CreateLogger();
                }).SingleInstance();

            builder.RegisterType<SailfishExecutor>().AsSelf();
            builder.RegisterType<SailTestExecutor>().As<ISailTestExecutor>();
            builder.RegisterType<TestFilter>().As<ITestFilter>();
            builder.RegisterType<TestListValidator>().As<ITestListValidator>();
            builder.RegisterType<TestCollector>().As<ITestCollector>();
            builder.RegisterType<ParameterCombinator>().As<IParameterCombinator>();
            builder.RegisterType<ParameterGridCreator>().As<IParameterGridCreator>();
            builder.RegisterType<TestInstanceContainerCreator>().As<ITestInstanceContainerCreator>();
            builder.RegisterType<TypeResolver>().As<ITypeResolver>();
            builder.RegisterType<TestCaseIterator>().As<ITestCaseIterator>();
            builder.RegisterType<StatisticsCompiler>().As<IStatisticsCompiler>();
            builder.RegisterType<TestResultCompiler>().As<ITestResultCompiler>();
            builder.RegisterType<TestResultPresenter>().As<ITestResultPresenter>();
            builder.RegisterType<PresentationStringConstructor>().As<IPresentationStringConstructor>().InstancePerDependency();
            builder.RegisterType<FileIo>().As<IFileIo>();
            builder.RegisterType<MarkdownWriter>().As<IMarkdownWriter>();
            builder.RegisterType<ConsoleWriter>().As<IConsoleWriter>();
            builder.RegisterType<PerformanceCsvWriter>().As<IPerformanceCsvWriter>();
            builder.RegisterType<TTest>().As<ITTest>();
            builder.RegisterType<TwoTailedTTestWriter>().As<ITwoTailedTTestWriter>();
            builder.RegisterType<TrackingFileFinder>().As<ITrackingFileFinder>();
            builder.RegisterType<PerformanceCsvTrackingWriter>().As<IPerformanceCsvTrackingWriter>();
            builder.RegisterType<IterationVariableRetriever>().As<IIterationVariableRetriever>();

        }
    }
}