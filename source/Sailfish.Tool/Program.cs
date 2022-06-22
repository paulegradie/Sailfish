using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using McMaster.Extensions.CommandLineUtils;
using Sailfish.Tool.Framework.DIContainer;

namespace Sailfish.Tool
{
    internal class Program
    {
        [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
        public string[] TestNames { get; set; } = { };

        [Option("-o|--outputDir", CommandOptionType.SingleValue, Description = "Path to an output directory. Absolute or relative.")]
        public string OutputDirectory { get; set; }

        [Option("-n|--no-track", CommandOptionType.SingleValue, Description = "Disable tracking. Tracking is where we emit results to enabled targets for later reference when performing statistical analysis.")]
        public bool NoTrack { get; set; }

        [Option("-a|--analyze", CommandOptionType.SingleValue, Description = "Use this option to enable analysis mode, where a directory is nominated, and it is used to track and retrieve historical performance test runs for use in statistical tests against new runs.")]
        public bool Analyze { get; set; }


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
                .Resolve<SailfishExecutor>()
                .Run(TestNames.Where(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x)).ToArray(), OutputDirectory, NoTrack, Analyze);
        }
    }
}