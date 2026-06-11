using System.IO;
using PerformanceTestingUserInvokedConsoleApp;
using PerformanceTests;
using PerformanceTests.ExamplePerformanceTests;
using Sailfish;
using Sailfish.Analysis.SailDiff;
using Sailfish.Logging;
using Serilog.Events;

const string outputDirectory = "my_custom_directory";

// SailDiff does not auto-compare against your previous run. To opt into a before/after comparison, resolve
// the previous run's tracking file explicitly (before this run writes its own) and hand it to the builder.
// On the first ever run there is nothing to compare against, so this is null and SailDiff just records the run.
var trackingDirectory = Path.Combine(outputDirectory, TrackingFiles.DefaultTrackingDirectoryName);
var previousRun = TrackingFiles.MostRecentIn(trackingDirectory);

var builder = RunSettingsBuilder
    .CreateBuilder()
    .TestsFromAssembliesContaining(typeof(PerformanceTestProjectDiscoveryAnchor))
    .ProvidersFromAssembliesContaining(typeof(AppRegistrationProvider))
    .WithTestNames(typeof(ReadmeExample).FullName!)
    .WithSailDiff()
    .WithScaleFish()
    .WithAiAnalysis() // Skipper: explains SailDiff results via the registered transport (see AppRegistrationProvider)
    // .WithGlobalSampleSize(30)
    .WithMinimumLogLevel(LogLevel.Information)
    // .WithCustomLogger(new CustomLogger(new LoggerConfiguration().WriteTo.Console().CreateLogger()))
    // .DisableStreamingTrackingUpdates()
    .WithLocalOutputDirectory(outputDirectory);

// Opt in to the historical comparison only when a previous run exists.
if (previousRun is not null)
    builder = builder.WithProvidedBeforeTrackingFile(previousRun);

var settings = builder.Build();

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