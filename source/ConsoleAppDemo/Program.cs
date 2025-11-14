using PerformanceTestingUserInvokedConsoleApp;
using PerformanceTests;
using PerformanceTests.ExamplePerformanceTests;
using Sailfish;
using Sailfish.Logging;
using Serilog.Events;
using System;

var settings = RunSettingsBuilder
    .CreateBuilder()
    .TestsFromAssembliesContaining(typeof(PerformanceTestProjectDiscoveryAnchor))
    .ProvidersFromAssembliesContaining(typeof(AppRegistrationProvider))
    .WithTestNames(typeof(ReadmeExample).FullName!)
    .WithSailDiff()
    .WithScaleFish()
    // .WithGlobalSampleSize(30)
    .WithMinimumLogLevel(LogLevel.Information)
    // .WithCustomLogger(new CustomLogger(new LoggerConfiguration().WriteTo.Console().CreateLogger()))
    // .DisableStreamingTrackingUpdates()
    .WithLocalOutputDirectory("my_custom_directory")
    .Build();

var result = await SailfishRunner.Run(settings);
var not = result.IsValid ? string.Empty : "not ";
Console.WriteLine($"Test run was {not}valid");

namespace PerformanceTestingUserInvokedConsoleApp
{
    internal class CustomLogger : ILogger
    {
        private readonly Serilog.ILogger _logger;

        public CustomLogger(Serilog.ILogger seriLogger)
        {
            _logger = seriLogger;
        }

        public void Log(LogLevel level, string template, params object[] values)
        {
            _logger.Write(GetEventLevel(level), template, values);
        }

        public void Log(LogLevel level, Exception ex, string template, params object[] values)
        {
            _logger.Write(GetEventLevel(level), ex, template, values);
        }

        private static LogEventLevel GetEventLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Fatal => LogEventLevel.Fatal,
                _ => LogEventLevel.Verbose
            };
        }
    }
}