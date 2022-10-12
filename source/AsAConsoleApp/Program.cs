using System;
using System.Threading;
using System.Threading.Tasks;
using AsAConsoleApp.Configuration;
using Autofac;
using Sailfish;
using Sailfish.Program;

namespace AsAConsoleApp
{
    internal class Program : SailfishProgramBase
    {
        private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

        private static async Task Main(string[] userRequestedTestNames)
        {
            await SailfishMain<Program>(userRequestedTestNames);
        }

        public override async Task OnExecuteAsync()
        {
            var validityResult = await ContainerConfiguration
                .CompositionRoot()
                .Resolve<SailfishExecution>()
                .Run(AssembleRunRequest(), RegisterWithSailfish, cancellationToken);

            var it = validityResult.IsValid ? string.Empty : "not ";
            Console.WriteLine($"Test run was {it}valid");
        }

        public override void RegisterWithSailfish(ContainerBuilder builder)
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
                // tests and what then recorded in the cloud.
                case "notify":
                    builder.RegisterModule<OptionalAdditionalRegistrationsNotifications>();
                    break;
                case "cloud":
                    builder.RegisterModule<OptionalAdditionalRegistrationsCloud>();
                    break;
            }
        }
    }
}