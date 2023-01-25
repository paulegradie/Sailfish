using System;
using Autofac;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Registration;
using Serilog;
using Test.API;

namespace PerformanceTestingConsoleApp;

public class RegistrationProvider : IProvideARegistrationCallback
{
    public void Register(ContainerBuilder builder)
    {
        // These registrations will be used by Sailfish's internal DI container which
        // is necessary to resolve dependencies used by test classes.
        // Additionally, there are various MediatR handlers that can be overriden
        // using these additional registrations.
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
        switch (Environment.GetEnvironmentVariable("environment")?.ToLowerInvariant())
        {
            // These registrations can override the default handlers for
            // writing t-test results and reading/writing tracking files
            // This is useful if you've got a system for running automated perf
            // tests that record data to the cloud or some other non-default target.
            case "notify":
                builder.RegisterType<CustomNotificationHandler>().As<INotificationHandler<NotifyOnTestResultCommand>>();
                break;
            case "cloud":
                builder.RegisterType<CloudWriter>().As<ICloudWriter>();
                builder.RegisterType<CustomWriteToCloudHandler>().As<INotificationHandler<WriteCurrentTrackingFileCommand>>();
                break;
        }
    }
}