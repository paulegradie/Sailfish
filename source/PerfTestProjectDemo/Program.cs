using Autofac;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma.Executor;

namespace PerfTestProjectDemo;

internal class Program
{
    private static void Main(string[] userRequestedTestNames)
    {
        CommandLineApplication.Execute<Program>(userRequestedTestNames);
    }

    public void OnExecute()
    {
        ContainerConfiguration.CompositionRoot().Resolve<VeerPerformaExecutor>().Run(TestNames, typeof(CountToAMillionPerformance));
    }

    [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
    public string[] TestNames { get; set; } = new string[] { };
}