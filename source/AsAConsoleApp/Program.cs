using System.Threading.Tasks;
using Autofac;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma;

namespace AsAConsoleApp
{
    internal class Program
    {
        [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
        public string[] TestNames { get; set; } = { };

        private static async Task Main(string[] userRequestedTestNames)
        {
            await CommandLineApplication.ExecuteAsync<Program>(userRequestedTestNames);
        }

        public async Task OnExecute()
        {
            var logger = Logging.CreateLogger("ConsoleAppLogs.log");
            await ContainerConfiguration.CompositionRoot().Resolve<VeerPerformaExecutor>().Run(TestNames, typeof(DemoPerfTest));
        }
    }
}