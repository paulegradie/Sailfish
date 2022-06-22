using System.IO;
using System.Threading.Tasks;
using Autofac;
using McMaster.Extensions.CommandLineUtils;
using Sailfish;
using Sailfish.Presentation.TTest;

namespace AsAConsoleApp
{
    internal class Program
    {
        [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
        public string[] TestNames { get; set; } = { };

        [Option("-o|--outputDir", CommandOptionType.SingleValue, Description = "Path to an output directory. Absolute or relative.")]
        public string? OutputDirectory { get; set; }

        [Option("-n|--no-track", CommandOptionType.SingleValue, Description = "Disable tracking. Tracking is where we emit results to enabled targets for later reference when performing statistical analysis.")]
        public bool NoTrack { get; set; }

        [Option("-a|--analyze", CommandOptionType.SingleValue, Description = "Use this option to enable analysis mode, where a directory is nominated, and it is used to track and retrieve historical performance test runs for use in statistical tests against new runs.")]
        public bool Analyze { get; set; } = true;

        [Option("-h|--ttest-alpha", CommandOptionType.SingleValue, Description = "Use this option to set the significance threshold for the ttest analysis.")]
        public double Alpha { get; set; } = 0.5;


        private static async Task Main(string[] userRequestedTestNames)
        {
            await CommandLineApplication.ExecuteAsync<Program>(userRequestedTestNames);
        }

        public async Task OnExecute()
        {
            var logger = Logging.CreateLogger("ConsoleAppLogs.log");

            if (OutputDirectory is null)
            {
                OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "performance_output");
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }
            }

            await ContainerConfiguration
                .CompositionRoot()
                .Resolve<SailfishExecutor>()
                .Run(TestNames, OutputDirectory, NoTrack, Analyze, new TTestSettings(Alpha), GetType());
        }
    }
}