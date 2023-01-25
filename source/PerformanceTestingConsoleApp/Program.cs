using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PerformanceTests;
using Sailfish.Program;
using Serilog;

// ReSharper disable ClassNeverInstantiated.Global

namespace PerformanceTestingConsoleApp;

internal class Program : SailfishProgramBase
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
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
        // Types used to resolve tests and dependencies
        return new[] { typeof(PerformanceTestProjectDiscoveryAnchor), GetType() };
    }
}