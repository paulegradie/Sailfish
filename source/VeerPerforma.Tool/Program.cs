using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma.Tool.Framework.DIContainer;

namespace VeerPerforma.Tool
{
    internal class Program
    {
        [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
        public string[]? TestNames { get; set; } = {""};

        public static async Task<int> Main(string[] args)
        {
            return await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        public async Task OnExecute()
        {
            if (TestNames is null) throw new Exception("Program failed to start...");
            await ContainerConfiguration
                .CompositionRoot()
                .Resolve<VeerPerformaExecutor>()
                .Run(TestNames.Where(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x)).ToArray());
        }
    }
}