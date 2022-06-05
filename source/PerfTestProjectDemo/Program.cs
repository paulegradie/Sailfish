// See https://aka.ms/new-console-template for more information


using Autofac;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma.Executor;

namespace PerfTestProjectDemo;

internal class Program
{
    static void Main(string[] userRequestedTestNames)
    {
        CommandLineApplication.Execute<Program>(userRequestedTestNames);
    }

    public void OnExecute()
    {
        ContainerConfiguration.CompositionRoot().Resolve<VeerPerformaExecutor>().Run(TestNames);
    }

    [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
    public string[]? TestNames { get; set; }
}