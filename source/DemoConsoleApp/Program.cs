using Autofac;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma;

namespace PerfTestProjectDemo;

internal class Program
{
    private static async Task Main(string[] userRequestedTestNames)
    {
        await CommandLineApplication.ExecuteAsync<Program>(userRequestedTestNames);
    }

    public async Task OnExecute()
    {
        await ContainerConfiguration.CompositionRoot().Resolve<VeerPerformaExecutor>().Run(TestNames, typeof(CountToAMillionPerformance));
    }

    [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
    public string[] TestNames { get; set; } = new string[] { };
}