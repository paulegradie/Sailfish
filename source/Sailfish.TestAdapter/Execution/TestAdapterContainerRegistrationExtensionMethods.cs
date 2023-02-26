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
        builder.RegisterType<TestInstanceContainerCreator>().As<ITestInstanceContainerCreator>();
        builder.RegisterType<MarkdownTableConverter>().As<IMarkdownTableConverter>();
        builder.RegisterType<ExecutionSummaryCompiler>().As<IExecutionSummaryCompiler>();
        builder.RegisterType<StatisticsCompiler>().As<IStatisticsCompiler>();
        builder.RegisterType<TestCaseIterator>().As<ITestCaseIterator>();
        builder.RegisterType<TestAdapterExecutionProgram>().As<ITestAdapterExecutionProgram>();
        builder.RegisterType<TypeActivator>().As<ITypeActivator>();
        builder.Register(ctx => new SailfishExecutionEngine(ctx.Resolve<ITestCaseIterator>())).As<ISailfishExecutionEngine>();
        builder.RegisterType<ConsoleWriterFactory>().As<IConsoleWriterFactory>();
    }
}

// This is a thing I need to do temporarily until I can upgrade to autofac 6.5.
// 4.x does not support registering functions...
internal interface IConsoleWriterFactory
{
    ConsoleWriter CreateConsoleWriter(ITestExecutionRecorder? handle);
}

internal class ConsoleWriterFactory : IConsoleWriterFactory
{
    private readonly IMarkdownTableConverter markdownTableConverter;

    public ConsoleWriterFactory(IMarkdownTableConverter markdownTableConverter)
    {
        this.markdownTableConverter = markdownTableConverter;
    }

    public ConsoleWriter CreateConsoleWriter(ITestExecutionRecorder? handle)
    {
        return new ConsoleWriter(markdownTableConverter, handle);
    }
}