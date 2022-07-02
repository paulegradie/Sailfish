using System.IO;
using System.Threading.Tasks;
using AsAConsoleApp.Configuration;
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

        [Option("-k|--trackingDirectory", CommandOptionType.SingleValue, Description = "Path to an output directory. Absolute or relative.")]
        public string? TrackingDirectory { get; set; }

        [Option("-n|--no-track", CommandOptionType.SingleValue, Description = "Disable tracking. Tracking is where we emit results to enabled targets for later reference when performing statistical analysis.")]
        public bool NoTrack { get; set; }

        [Option("-a|--analyze", CommandOptionType.SingleValue, Description = "Use this option to enable analysis mode, where a directory is nominated, and it is used to track and retrieve historical performance test runs for use in statistical tests against new runs.")]
        public bool Analyze { get; set; } = true;

        [Option("-h|--ttest-alpha", CommandOptionType.SingleValue, Description = "Use this option to set the significance threshold for the ttest analysis.")]
        public double Alpha { get; set; } = 0.005;

        [Option("-r|--round", CommandOptionType.SingleValue, Description = "The number of digits to round to")]
        public int Round { get; set; } = 4;


        private static async Task Main(string[] userRequestedTestNames)
        {
            await CommandLineApplication.ExecuteAsync<Program>(userRequestedTestNames);
        }

        public async Task OnExecute()
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
            builder.RegisterModule<ExtraRegistrationsModule>();
        }

        private RunSettings AssembleRunRequest()
        {
            if (OutputDirectory is null)
            {
                OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "performance_output");
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }
            }

            return new RunSettings(TestNames, OutputDirectory, NoTrack, Analyze, new TTestSettings(Alpha, Round), GetType());
        }
    }
    
}