using PerformanceTestingUserInvokedConsoleApp;
using PerformanceTests;
using PerformanceTests.ExamplePerformanceTests;
using Sailfish;
using Sailfish.Logging;
using Serilog;
using ILogger = Sailfish.Logging.ILogger;

var settings = RunSettingsBuilder
    .CreateBuilder()
    .TestsFromAssembliesContaining(typeof(PerformanceTestProjectDiscoveryAnchor))
    .ProvidersFromAssembliesContaining(typeof(AppRegistrationProvider))
    // .WithTestNames(typeof(ExceptionExample).FullName!, typeof(MinimalTestExample).FullName)
    .WithSailDiff()
    .WithScaleFish()
    .WithGlobalSampleSize(5)
    .WithMinimumLogLevel(LogLevel.Information)
    // .WithCustomLogger(new CustomLogger(new LoggerConfiguration().WriteTo.Console().CreateLogger()))
    .DisableStreamingTrackingUpdates()
    .WithLocalOutputDirectory("my_custom_directory")
    .Build();

var result = await SailfishRunner.Run(settings);
var not = result.IsValid ? string.Empty : "not ";
Console.WriteLine($"Test run was {not}valid");

namespace PerformanceTestingUserInvokedConsoleApp
{
    internal class CustomLogger : ILogger
    {
        private readonly Serilog.ILogger logger;
        public CustomLogger(Serilog.ILogger seriLogger) => logger = seriLogger;
        public void Log(LogLevel level, string template, params object[] values) => logger.Write(GetEventLevel(level), template, values);
        public void Log(LogLevel level, Exception ex, string template, params object[] values) => logger.Write(GetEventLevel(level), ex, template, values);

        static Serilog.Events.LogEventLevel GetEventLevel(LogLevel level)
        {
            return level switch
            { 
            
                LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
                LogLevel.Information => Serilog.Events.LogEventLevel.Information,
                LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
                LogLevel.Error => Serilog.Events.LogEventLevel.Error,
                LogLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
                _ => Serilog.Events.LogEventLevel.Verbose
            };
        }
    }
}