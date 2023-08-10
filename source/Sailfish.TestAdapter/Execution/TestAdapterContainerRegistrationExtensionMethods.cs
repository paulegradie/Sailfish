using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.Saildiff;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Registration;
using Sailfish.Statistics;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

namespace Sailfish.TestAdapter.Execution;

internal static class TestAdapterContainerRegistrationExtensionMethods
{
    public static void CreateTestAdapterRegistrationContainerBuilder(this ContainerBuilder builder, IFrameworkHandle? frameworkHandle)
    {
        builder.RegisterSailfishTypes();

        // TODO: Deprecated these - we now need to register everything from sailfish - but we have some overrides, like IConsoleWRiterFactory
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
        builder.RegisterType<TTestSailfish>().As<ITTestSailfish>();
        builder.RegisterType<MannWhitneyWilcoxonTestSailfish>().As<IMannWhitneyWilcoxonTestSailfish>();
        builder.RegisterType<TwoSampleWilcoxonSignedRankTestSailfish>().As<ITwoSampleWilcoxonSignedRankTestSailfish>();
        builder.RegisterType<TestPreprocessor>().As<ITestPreprocessor>();

        builder.RegisterType<TTestSailfish>().As<ITTestSailfish>();
        builder.RegisterType<MannWhitneyWilcoxonTestSailfish>().As<IMannWhitneyWilcoxonTestSailfish>();
        builder.RegisterType<TwoSampleWilcoxonSignedRankTestSailfish>().As<ITwoSampleWilcoxonSignedRankTestSailfish>();
        builder.RegisterType<TestPreprocessor>().As<ITestPreprocessor>();
        builder.RegisterType<StatisticalTestExecutor>().As<IStatisticalTestExecutor>();
        builder.RegisterType<TestComputer>().As<ITestComputer>();


        // These need to be overriding registrations for the test adapter
        if (frameworkHandle is not null)
        {
            builder.RegisterInstance(frameworkHandle).As<IFrameworkHandle>();
        }

        builder.RegisterType<AdapterSailDiff>().As<IAdapterSailDiff>();
        builder.RegisterType<AdapterScaleFish>().As<IAdapterScaleFish>();
        builder.RegisterType<AdapterConsoleWriter>().As<IAdapterConsoleWriter>();
        builder.RegisterType<TestAdapterExecutionEngine>().As<ITestAdapterExecutionEngine>();
    }
}