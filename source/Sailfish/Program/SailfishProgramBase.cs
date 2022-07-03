using System.IO;
using System.Threading.Tasks;
using Autofac;
using McMaster.Extensions.CommandLineUtils;
using Sailfish.Presentation.TTest;
using Sailfish.Utils;

namespace Sailfish.Program;

public abstract class SailfishProgramBase
{
    public static async Task SailfishMain<TProgram>(string[] userRequestedTestNames) where TProgram : class
    {
        await CommandLineApplication.ExecuteAsync<TProgram>(userRequestedTestNames);
    }

    public abstract Task OnExecuteAsync();
    public abstract void RegisterWithSailfish(ContainerBuilder builder);

    public RunSettings AssembleRunRequest()
    {
        if (OutputDirectory is null)
        {
            OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "performance_output");
            Directories.EnsureDirectoryExists(OutputDirectory);
        }

        if (TrackingDirectory is null)
        {
            TrackingDirectory = Path.Combine(Directory.GetCurrentDirectory(), OutputDirectory, "tracking_directory");
            Directories.EnsureDirectoryExists(TrackingDirectory);
        }

        return new RunSettings(TestNames, OutputDirectory, TrackingDirectory, NoTrack, Analyze, Notify, new TTestSettings(Alpha, Round), GetType());
    }

    [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
    public string[] TestNames { get; set; } = { };

    [Option("-o|--outputDir", CommandOptionType.SingleValue, Description = "Path to an output directory. Absolute or relative")]
    public string? OutputDirectory { get; set; }

    [Option("-k|--trackingDirectory", CommandOptionType.SingleValue, Description = "Path to an output directory for tracking files. Absolute or relative")]
    public string? TrackingDirectory { get; set; }

    [Option("-n|--no-track", CommandOptionType.NoValue, Description = "Disable tracking. Tracking files are used when performing statistical analysis")]
    public bool NoTrack { get; set; } = false;

    [Option("-a|--analyze", CommandOptionType.NoValue, Description = "Use this option to enable analysis mode, where a directory is nominated, and it is used to track and retrieve historical performance test runs for use in statistical tests against new runs")]
    public bool Analyze { get; set; } = true;

    [Option("-y|--notify", CommandOptionType.NoValue, Description = "Use this option to enable sending of the notification command. There are no default handlers, but users can implement their own to process ttest results and send messages to webhooks in response")]
    public bool Notify { get; set; } = true;

    [Option("-h|--ttest-alpha", CommandOptionType.SingleValue, Description = "Use this option to set the significance threshold for the ttest analysis")]
    public double Alpha { get; set; } = 0.005;

    [Option("-r|--round", CommandOptionType.SingleValue, Description = "The number of digits to round to")]
    public int Round { get; set; } = 4;

    [Option("-e|--environment", CommandOptionType.SingleValue, Description = "A flag you can use to specify a runtime environment. This can be used, e.g., to switch registrations.")]
    public string? Environment { get; set; }
}