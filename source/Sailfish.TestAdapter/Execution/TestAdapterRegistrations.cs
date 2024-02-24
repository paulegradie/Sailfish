using Autofac;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Private.ExecutionCallbackNotifications;
using Sailfish.Logging;
using Sailfish.Registration;
using Sailfish.TestAdapter.FrameworkHandlers;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterRegistrations : IProvideAdditionalRegistrations
{
    private readonly IFrameworkHandle? frameworkHandle;

    public TestAdapterRegistrations(IFrameworkHandle? frameworkHandle)
    {
        this.frameworkHandle = frameworkHandle;
    }

    public void Load(ContainerBuilder builder)
    {
        if (frameworkHandle is not null) builder.RegisterInstance(frameworkHandle).As<IFrameworkHandle>();

        builder.RegisterType<TestAdapterExecutionProgram>().As<ITestAdapterExecutionProgram>();
        builder.RegisterType<TestAdapterExecutionEngine>().As<ITestAdapterExecutionEngine>();
        builder.RegisterType<AdapterConsoleWriter>().As<IAdapterConsoleWriter>();
        builder.RegisterType<AdapterSailDiff>().As<ISailDiffInternal>().InstancePerDependency();
        builder.RegisterType<AdapterSailDiff>().As<IAdapterSailDiff>();
        builder.RegisterType<AdapterScaleFish>().As<IScaleFishInternal>();
        builder.RegisterType<AdapterScaleFish>().As<IAdapterScaleFish>();
        builder.RegisterType<TestCaseCountPrinter>().As<ITestCaseCountPrinter>().SingleInstance();

        builder.RegisterType<ExecutionStartingNotificationHandler>()
            .As<INotificationHandler<ExecutionStartingNotification>>();
        builder.RegisterType<ExecutionCompletedNotificationHandler>()
            .As<INotificationHandler<ExecutionCompletedNotification>>();
        builder.RegisterType<ExecutionDisabledNotificationHandler>()
            .As<INotificationHandler<ExecutionDisabledNotification>>();
        builder.RegisterType<ExceptionNotificationHandler>().As<INotificationHandler<ExceptionNotification>>();
    }
}