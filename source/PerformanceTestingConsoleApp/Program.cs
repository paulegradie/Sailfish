using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PerformanceTests;
using Sailfish.Presentation;
using Sailfish.Program;
using Serilog;

// ReSharper disable ClassNeverInstantiated.Global

namespace PerformanceTestingConsoleApp;

public class Program : SailfishProgramBase
{
    public static async Task Main(string[] userRequestedTestNames)
    {
        // your main can call the sailfish main.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", true)
            .Build();
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

        await SailfishMain<Program>(userRequestedTestNames);

        // You can access your run result using this global static property if you'd like
        if (RunResult.IsValid)
        {
            var executionSummaries = RunResult.ExecutionSummaries;
            var markdown = new MarkdownTableConverter().ConvertToMarkdownTableString(executionSummaries);
            Console.WriteLine(markdown);
        }
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
        // Types used to resolve tests and dependencies
        return new[] { typeof(PerformanceTestProjectDiscoveryAnchor) };
    }

    protected override IEnumerable<Type> RegistrationProviderTypesProvider()
    {
        // Types used to resolve registration providers
        return new[] { typeof(RegistrationProvider) };
    }
}