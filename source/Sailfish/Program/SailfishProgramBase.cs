﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Collections;
using Autofac;
using McMaster.Extensions.CommandLineUtils;
using Sailfish.Analysis;
using Sailfish.Utils;

namespace Sailfish.Program;

public class SailfishProgramBase
{
    protected static async Task SailfishMain<TProgram>(string[] userRequestedTestNames) where TProgram : class
    {
        await CommandLineApplication.ExecuteAsync<TProgram>(userRequestedTestNames);
    }

    protected async Task OnExecuteAsync(CancellationToken cancellationToken)
    {
        var validityResult = await SailfishRunner.Run(AssembleRunRequest(SourceTypesProvider()), RegisterWithSailfish, cancellationToken);
        var not = validityResult.IsValid ? string.Empty : "not ";
        Console.WriteLine($"Test run was {not}valid");
    }

    protected virtual IEnumerable<Type> SourceTypesProvider()
    {
        return Enumerable.Empty<Type>();
    }

    protected virtual void RegisterWithSailfish(ContainerBuilder builder)
    {
    }

    protected virtual RunSettings AssembleRunRequest(IEnumerable<Type> sourceTypes)
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

        var parsedTags = new OrderedDictionary<string, string>();
        if (Tags is not null)
        {
            parsedTags = ColonParser.Parse(Tags);
        }

        var parsedArgs = new OrderedDictionary<string, string>();
        if (Args is not null)
        {
            parsedArgs = ColonParser.Parse(Args);
        }

        BeforeTarget ??= string.Empty;

        DateTime? timestamp = null;
        if (TimeStamp is not null)
        {
            timestamp = DateTime.Parse(TimeStamp);
        }

        return new RunSettings(
            TestNames,
            OutputDirectory,
            TrackingDirectory,
            NoTrack,
            Analyze,
            Notify,
            new TestSettings(Alpha, Round, useInnerQuartile: true),
            parsedTags,
            parsedArgs,
            BeforeTarget,
            timestamp,
            Debug,
            sourceTypes.ToArray());
    }

    [Option("-a|--analyze", CommandOptionType.NoValue,
        Description =
            "Use this option to enable analysis mode, where a directory is nominated, and it is used to track and retrieve historical performance test runs for use in statistical tests against new runs")]
    public bool Analyze { get; set; } = true;

    [Option("-b|--before-target", CommandOptionType.SingleValue,
        Description =
            "A file name use to filter a specific tracking file for comparison when executing. This arg is passed to the BeforeAndAfterFileLocationCommand")]
    public string? BeforeTarget { get; set; }

    [Option("-e|--environment", CommandOptionType.SingleValue,
        Description =
            "A flag you can use to specify a runtime environment. This can be used, e.g., to switch registrations.")]
    public string? Environment { get; set; }

    [Option("-g|--tag", CommandOptionType.MultipleValue,
        Description =
            "A series of colon separated values that provide a key:value relationship. Use like -g version:123 -g build:2022.2.123")]
    public string[]? Tags { get; set; }

    [Option("-h|--test-alpha", CommandOptionType.SingleValue,
        Description = "Use this option to set the significance threshold for the test analysis")]
    public double Alpha { get; set; } = 0.005;

    [Option("-k|--trackingDirectory", CommandOptionType.SingleValue,
        Description = "Path to an output directory for tracking files. Absolute or relative")]
    public string? TrackingDirectory { get; set; }

    [Option("-m|--timeStamp", CommandOptionType.SingleValue,
        Description =
            "String to use as the timestamp. This should be a time sortable format. Input should be parsable by `DateTime.Parse`")]
    public string? TimeStamp { get; set; }

    [Option("-n|--no-track", CommandOptionType.NoValue,
        Description = "Disable tracking. Tracking files are used when performing statistical analysis")]
    public bool NoTrack { get; set; } = false;

    [Option("-o|--outputDir", CommandOptionType.SingleValue,
        Description = "Path to an output directory. Absolute or relative")]
    public string? OutputDirectory { get; set; }

    [Option("-r|--round", CommandOptionType.SingleValue, Description = "The number of digits to round to")]
    public int Round { get; set; } = 4;

    [Option("-s|--args", CommandOptionType.MultipleValue,
        Description =
            "A series of colon separated values that provide a key:value relationship. This collection is not used internally by Sailfish, but is provided to all command handlers. All args are parsed as string. Use like -s runName:test-run -s use-main:true")]
    public string[]? Args { get; set; }

    [Option("-t|--tests", CommandOptionType.MultipleValue, Description = "List of tests to execute")]
    public string[] TestNames { get; set; } = Array.Empty<string>();

    [Option("-y|--notify", CommandOptionType.NoValue,
        Description =
            "Use this option to enable sending of the notification command. There are no default handlers, but users can implement their own to process test results and send messages to webhooks in response")]
    public bool Notify { get; set; } = true;

    [Option("-d|--debug", CommandOptionType.NoValue, Description = "Use this option to enable debug mode. This will provide more verbose errors in your outputs")]
    public bool Debug { get; set; } = false;
}