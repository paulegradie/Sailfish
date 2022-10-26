using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsAConsoleApp.CustomHandlerOverrideExamples;
using Autofac;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Program;
using Serilog;
using Test.API;

namespace AsAConsoleApp;

internal class Program : SailfishProgramBase
{
    public static async Task Main(string[] userRequestedTestNames)
    {
        // your main can call the sailfish main.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", true)
            .Build();

        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

        await SailfishMain<Program>(userRequestedTestNames);

        // alternatively, if you don't want the base / cli tools, you can do
        // await SailfishRunner.Run(new RunSettings(), RegisterWithSailfishCallback, cancellationToken);
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
        // any types from any assembly that contains Sailfish tests
        // to direct a scan of the assembly
        return new[] { GetType() };
    }

    protected override void RegisterWithSailfish(ContainerBuilder builder)
    {
        // These registrations will be used by Sailfish's internal DI container which
        // is necessary to resolve dependencies used by test classes.
        // Additionally, there are various MediatR handlers that can be overriden
        // using these additional registrations.
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
        switch (Environment?.ToLowerInvariant())
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