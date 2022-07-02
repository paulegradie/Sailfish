using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using McMaster.Extensions.CommandLineUtils;
using Sailfish.Presentation.TTest;
using Sailfish.Tool.Framework.DIContainer;

namespace Sailfish.Tool
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
        public bool Analyze { get; set; }

        [Option("-h|--ttest-alpha", CommandOptionType.SingleValue, Description = "Use this option to set the significance threshold for the ttest analysis.")]
        public double Alpha { get; set; } = 0.01;

        [Option("-r|--round", CommandOptionType.SingleValue, Description = "The number of digits to round to")]
        public int Round { get; set; } = 4;

        public static async Task<int> Main(string[] args)
        {
            return await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        public async Task OnExecute()
        {
            if (OutputDirectory is null)
            {
                OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "performance_output");
            }

            if (TestNames is null) throw new Exception("Program failed to start...");

            await ContainerConfiguration
                .CompositionRoot()
                .Resolve<SailfishExecution>()
                .Run(AssembleRunRequest());
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