using Autofac;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma;

namespace AsAConsoleApp;

internal class Program
{
    private static async Task Main(string[] userRequestedTestNames)
    {
        await CommandLineApplication.ExecuteAsync<Program>(userRequestedTestNames);
    }

    public async Task OnExecute()
    {
        var logger = Logging.CreateLogger("ConsoleAppLogs.log");
        logger.Information("Oh mai - we have the logging to seq finally.");
        await ContainerConfiguration.CompositionRoot().Resolve<VeerPerformaExecutor>().Run(TestNames, typeof(DemoPerfTest));
    }

    [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
    public string[] TestNames { get; set; } = new string[] { };
}