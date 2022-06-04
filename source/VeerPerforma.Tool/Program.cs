// See https://aka.ms/new-console-template for more information

using Autofac;
using McMaster.Extensions.CommandLineUtils;
using VeerPerforma.Tool.Framework.DIContainer;

namespace VeerPerforma.Tool;

class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        return CommandLineApplication.Execute<Program>(args);
    }

    public void OnExecute()
    {
        ContainerConfiguration.CompositionRoot().Resolve<VeerPerformaExecutor>().Run(TestNames);
    }

    [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
    public string[]? TestNames { get; set; }
}