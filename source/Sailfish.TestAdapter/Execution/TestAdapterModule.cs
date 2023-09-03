using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Statistics;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterModule : Module
{
    private readonly IFrameworkHandle? frameworkHandle;

    public TestAdapterModule(IFrameworkHandle? frameworkHandle)
    {
        this.frameworkHandle = frameworkHandle;
    }

    protected override void Load(ContainerBuilder builder)
    {
        if (frameworkHandle is not null)
        {
            builder.RegisterInstance(frameworkHandle).As<IFrameworkHandle>();
        }

        builder.RegisterType<TestAdapterExecutionProgram>().As<ITestAdapterExecutionProgram>();
        builder.RegisterType<AdapterActivatorCallbacks>().As<IActivatorCallbacks>();
        builder.RegisterType<AdapterSailDiff>().As<IAdapterSailDiff>();
        builder.RegisterType<AdapterScaleFish>().As<IAdapterScaleFish>();
        builder.RegisterType<AdapterConsoleWriter>().As<IAdapterConsoleWriter>();
        builder.RegisterType<TestAdapterExecutionEngine>().As<ITestAdapterExecutionEngine>();
    }
}