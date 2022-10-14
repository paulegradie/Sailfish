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
        await SailfishMain<Program>(userRequestedTestNames);
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
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