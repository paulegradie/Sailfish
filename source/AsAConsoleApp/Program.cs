using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsAConsoleApp.Configuration;
using Autofac;
using Sailfish.Program;

namespace AsAConsoleApp;

internal class Program : SailfishProgramBase
{
    public static async Task Main(string[] userRequestedTestNames)
    {
        // your main can call the sailfish main.
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
        builder.RegisterModule<RequiredAdditionalRegistrationsModule>();

        switch (Environment?.ToLowerInvariant())
        {
            // These registrations can override the default handlers for
            // writing t-test results and reading/writing tracking files
            // This is useful if you've got a system for running automated perf
            // tests that record data to the cloud or some other non-default target.
            case "notify":
                builder.RegisterModule<OptionalAdditionalRegistrationsNotifications>();
                break;
            case "cloud":
                builder.RegisterModule<OptionalAdditionalRegistrationsCloud>();
                break;
        }
    }
}
