using System.Threading.Tasks;
using AsAConsoleApp.Configuration;
using Autofac;
using Sailfish;
using Sailfish.Program;

namespace AsAConsoleApp
{
    class Program : SailfishProgramBase
    {
        private static async Task Main(string[] userRequestedTestNames)
        {
            await SailfishMain<Program>(userRequestedTestNames);
        }

        public override async Task OnExecuteAsync()
        {
            await ContainerConfiguration
                .CompositionRoot()
                .Resolve<SailfishExecution>()
                .Run(AssembleRunRequest(), RegisterWithSailfish);
        }

        public void RegisterWithSailfish(ContainerBuilder builder)
        {
            // These registrations will be used by Sailfish's internal DI container which
            // is necessary to resolve dependencies used by test classes.
            // Additionally, there are various MediatR handlers that can be overriden
            // using these additional registrations.
            builder.RegisterModule<RequiredAdditionalRegistrationsModule>();

            // These registrations can override the default handlers for
            // writing t-test results and reading/writing tracking files
            // This is useful if you've got a system for running automated perf
            // tests and what then recorded in the cloud.
            if (Environment?.ToLowerInvariant() == "notify")
            {
                builder.RegisterModule<OptionalAdditionalRegistrationsNotifications>();
            }
            else if (Environment?.ToLowerInvariant() == "cloud")
            {
                builder.RegisterModule<OptionalAdditionalRegistrationsCloud>();
            }
        }
    }
}