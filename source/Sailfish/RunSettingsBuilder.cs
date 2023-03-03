using System;
using System.Collections.Generic;
using Accord.Collections;
using Sailfish.Analysis;
using Sailfish.Presentation;


namespace Sailfish;

public class RunSettingsBuilder
{
    private bool createTrackingFiles = true;
    private bool analyze = false;
    private bool executeNotificationHandler = false;
    private readonly List<string> names = new();
    private readonly List<Type> testAssembliesAnchorTypes = new();
    private readonly List<Type> registrationProviderAnchorTypes = new();
    private readonly List<string> providedBeforeTrackingFiles = new();
    private OrderedDictionary<string, string> tags = new();
    private OrderedDictionary<string, string> args = new();
    private string? localOutputDir;
    private TestSettings? tSettings;
    private DateTime? timeStamp;
    private bool debg = false;

    public static RunSettingsBuilder CreateBuilder()
    {
        return new RunSettingsBuilder();
    }

    public RunSettingsBuilder WithTestNames(params string[] testNames)
    {
        this.names.AddRange(testNames);
        return this;
    }

    public RunSettingsBuilder WithLocalOutputDirectory(string localOutputDirectory)
    {
        this.localOutputDir = localOutputDirectory;
        return this;
    }

    public RunSettingsBuilder CreateTrackingFiles(bool track = true)
    {
        this.createTrackingFiles = track;
        return this;
    }

    public RunSettingsBuilder WithAnalysis()
    {
        this.analyze = true;
        return this;
    }

    public RunSettingsBuilder ExecuteNotificationHandler()
    {
        this.executeNotificationHandler = true;
        return this;
    }

    public RunSettingsBuilder WithAnalysisTestSettings(TestSettings testSettings)
    {
        this.tSettings = testSettings;
        return this;
    }

    public RunSettingsBuilder TestsFromAssembliesFromAnchorType(Type anchorType)
    {
        this.testAssembliesAnchorTypes.Add(anchorType);
        return this;
    }

    public RunSettingsBuilder TestsFromAssembliesFromAnchorTypes(params Type[] anchorTypes)
    {
        this.testAssembliesAnchorTypes.AddRange(anchorTypes);
        return this;
    }

    public RunSettingsBuilder RegistrationProvidersFromAssembliesFromAnchorType(Type anchorType)
    {
        this.registrationProviderAnchorTypes.Add(anchorType);
        return this;
    }

    public RunSettingsBuilder RegistrationProvidersFromAssembliesFromAnchorTypes(params Type[] anchorTypes)
    {
        this.registrationProviderAnchorTypes.AddRange(anchorTypes);
        return this;
    }

    public RunSettingsBuilder WithTag(string key, string value)
    {
        this.tags.Add(key, value);
        return this;
    }

    public RunSettingsBuilder WithTags(OrderedDictionary<string, string> tags)
    {
        this.tags = tags;
        return this;
    }


    public RunSettingsBuilder WithArg(string key, string value)
    {
        this.args.Add(key, value);
        return this;
    }

    public RunSettingsBuilder WithArgs(OrderedDictionary<string, string> args)
    {
        this.args = args;
        return this;
    }

    public RunSettingsBuilder WithProvidedBeforeTrackingFile(string trackingFile)
    {
        this.providedBeforeTrackingFiles.Add(trackingFile);
        return this;
    }

    public RunSettingsBuilder WithProvidedBeforeTrackingFiles(IEnumerable<string> trackingFiles)
    {
        this.providedBeforeTrackingFiles.AddRange(trackingFiles);
        return this;
    }

    public RunSettingsBuilder WithTimeStamp(DateTime dateTime)
    {
        this.timeStamp = dateTime;
        return this;
    }

    public RunSettingsBuilder InDebugMode(bool debug = false)
    {
        this.debg = debug;
        return this;
    }

    public IRunSettings Build()
    {
        return new RunSettings(
            names,
            localOutputDir ?? DefaultFileSettings.DefaultOutputDirectory,
            createTrackingFiles,
            analyze,
            executeNotificationHandler,
            tSettings ?? new TestSettings(),
            tags,
            args,
            providedBeforeTrackingFiles,
            timeStamp,
            testAssembliesAnchorTypes.Count == 0 ? new[] { GetType() } : testAssembliesAnchorTypes,
            registrationProviderAnchorTypes.Count == 0 ? new[] { GetType() } : registrationProviderAnchorTypes,
            debg
        );
    }
}