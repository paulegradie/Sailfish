using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;

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
        builder.RegisterType<TestAdapterExecutionEngine>().As<ITestAdapterExecutionEngine>();
        builder.RegisterType<AdapterActivatorCallbacks>().As<IActivatorCallbacks>();
        builder.RegisterType<AdapterConsoleWriter>().As<IAdapterConsoleWriter>();
        builder.RegisterType<AdapterSailDiff>().As<ISailDiff>();
        builder.RegisterType<AdapterSailDiff>().As<IAdapterSailDiff>();
        builder.RegisterType<AdapterScaleFish>().As<IScaleFish>();
        builder.RegisterType<AdapterScaleFish>().As<IAdapterScaleFish>();
    }
}