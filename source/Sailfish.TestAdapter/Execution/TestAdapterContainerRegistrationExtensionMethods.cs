using System;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Statistics;

namespace Sailfish.TestAdapter.Execution;

internal static class TestAdapterContainerRegistrationExtensionMethods
{
    public static void CreateTestAdapterRegistrationContainerBuilder(this ContainerBuilder builder)
    {
        builder.RegisterType<IterationVariableRetriever>().As<IIterationVariableRetriever>();
        builder.RegisterType<ParameterCombinator>().As<IParameterCombinator>();
        builder.RegisterType<PropertySetGenerator>().As<IPropertySetGenerator>();
        builder.RegisterType<TestInstanceContainerCreator>().As<TestInstanceContainerCreator>();
        builder.RegisterType<Func<ITestExecutionRecorder?, ConsoleWriter>>().AsSelf();
        builder.RegisterType<MarkdownTableConverter>().As<IMarkdownTableConverter>();
        builder.RegisterType<ExecutionSummaryCompiler>().As<IExecutionSummaryCompiler>();
        builder.RegisterType<StatisticsCompiler>().As<IStatisticsCompiler>();
        builder.RegisterType<TestCaseIterator>().As<ITestCaseIterator>();
        builder.Register(ctx =>
        {
            var iterator = ctx.Resolve<ITestCaseIterator>();
            return new SailfishExecutionEngine(iterator);
        }).As<ISailfishExecutionEngine>();
    }
}