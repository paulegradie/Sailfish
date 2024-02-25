using Autofac;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation.Console;
using Sailfish.Registration;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Handlers.TestCaseEvents;
using ConsoleWriter = Sailfish.TestAdapter.Display.TestOutputWindow.ConsoleWriter;

namespace Sailfish.TestAdapter.Registrations;

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
        builder.RegisterType<ConsoleWriter>().As<IConsoleWriter>();
        builder.RegisterType<AdapterSailDiff>().As<ISailDiffInternal>().InstancePerDependency();
        builder.RegisterType<AdapterSailDiff>().As<IAdapterSailDiff>();
        builder.RegisterType<AdapterScaleFish>().As<IScaleFishInternal>();
        builder.RegisterType<AdapterScaleFish>().As<IAdapterScaleFish>();
        builder.RegisterType<TestCaseCountPrinter>().As<ITestCaseCountPrinter>().SingleInstance();
        builder.RegisterType<TestFrameworkWriter>().As<ITestFrameworkWriter>().SingleInstance();
        builder.RegisterType<SailDiffTestOutputWindowMessageFormatter>().As<ISailDiffTestOutputWindowMessageFormatter>();
        builder.RegisterType<SailfishConsoleWindowFormatter>().As<ISailfishConsoleWindowFormatter>();

        builder.RegisterType<TestCaseStartedNotificationHandler>().As<INotificationHandler<TestCaseStartedNotification>>();
        builder.RegisterType<TestCaseCompletedNotificationHandler>().As<INotificationHandler<TestCaseCompletedNotification>>();
        builder.RegisterType<TestCaseDisabledNotificationHandler>().As<INotificationHandler<TestCaseDisabledNotification>>();
        builder.RegisterType<TestCaseExceptionNotificationHandler>().As<INotificationHandler<TestCaseExceptionNotification>>();

        builder.RegisterType<FrameworkTestCaseEndNotificationHandler>().As<INotificationHandler<FrameworkTestCaseEndNotification>>();
        
        
    }
}