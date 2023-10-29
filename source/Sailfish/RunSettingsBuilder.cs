using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;


namespace Sailfish;

public class RunSettingsBuilder
{
    private bool createTrackingFiles = true;
    private bool sailfDiff = false;
    private bool executeNotificationHandler = false;
    private readonly List<string> names = new();
    private readonly List<Type> testAssembliesAnchorTypes = new();
    private readonly List<Type> registrationProviderAnchorTypes = new();
    private readonly List<string> providedBeforeTrackingFiles = new();
    private OrderedDictionary tags = new();
    private OrderedDictionary args = new();
    private string? localOutputDir;
    private SailDiffSettings? sdSettings;
    private DateTime? timeStamp;
    private bool debg = false;
    private bool scaleFish = false;
    private bool disableOverheadEstimation;
    private bool disableAnalysisGlobally = false;
    private int? globalSampleSize;
    private int? globalNumWarmupIterations;

    public static RunSettingsBuilder CreateBuilder()
    {
        return new RunSettingsBuilder();
    }

    public RunSettingsBuilder WithTestNames(params string[] testNames)
    {
        names.AddRange(testNames);
        return this;
    }

    public RunSettingsBuilder WithLocalOutputDirectory(string localOutputDirectory)
    {
        localOutputDir = localOutputDirectory;
        return this;
    }

    public RunSettingsBuilder CreateTrackingFiles(bool track = true)
    {
        createTrackingFiles = track;
        return this;
    }

    public RunSettingsBuilder WithSailDiff()
    {
        sailfDiff = true;
        return this;
    }

    public RunSettingsBuilder WithSailDiff(SailDiffSettings settings)
    {
        sdSettings = settings;
        sailfDiff = true;
        return this;
    }

    public RunSettingsBuilder WithScalefish()
    {
        scaleFish = true;
        return this;
    }

    public RunSettingsBuilder ExecuteNotificationHandler()
    {
        executeNotificationHandler = true;
        return this;
    }

    public RunSettingsBuilder TestsFromAssembliesContaining(params Type[] anchorTypes)
    {
        testAssembliesAnchorTypes.AddRange(anchorTypes);
        return this;
    }

    public RunSettingsBuilder ProvidersFromAssembliesContaining(params Type[] anchorTypes)
    {
        registrationProviderAnchorTypes.AddRange(anchorTypes);
        return this;
    }

    public RunSettingsBuilder WithTag(string key, string value)
    {
        tags.Add(key, value);
        return this;
    }

    public RunSettingsBuilder WithTags(OrderedDictionary tags)
    {
        this.tags = tags;
        return this;
    }


    public RunSettingsBuilder WithArg(string key, string value)
    {
        args.Add(key, value);
        return this;
    }

    public RunSettingsBuilder WithArgs(OrderedDictionary args)
    {
        this.args = args;
        return this;
    }

    public RunSettingsBuilder WithProvidedBeforeTrackingFile(string trackingFile)
    {
        providedBeforeTrackingFiles.Add(trackingFile);
        return this;
    }

    public RunSettingsBuilder WithProvidedBeforeTrackingFiles(IEnumerable<string> trackingFiles)
    {
        providedBeforeTrackingFiles.AddRange(trackingFiles);
        return this;
    }

    public RunSettingsBuilder WithTimeStamp(DateTime dateTime)
    {
        timeStamp = dateTime;
        return this;
    }

    public RunSettingsBuilder InDebugMode(bool debug = false)
    {
        debg = debug;
        return this;
    }

    public RunSettingsBuilder DisableOverheadEstimation()
    {
        disableOverheadEstimation = true;
        return this;
    }

    public RunSettingsBuilder WithAnalysisDisabledGlobally()
    {
        disableAnalysisGlobally = true;
        return this;
    }

    public RunSettingsBuilder WithGlobalSampleSize(int sampleSize)
    {
        globalSampleSize = Math.Max(sampleSize, 1);
        return this;
    }

    public RunSettingsBuilder WithGlobalNumWarmupIterations(int numIterations)
    {
        globalNumWarmupIterations = Math.Max(numIterations, 1);
        return this;
    }

    public IRunSettings Build()
    {
        return new RunSettings(
            names,
            localOutputDir ?? DefaultFileSettings.DefaultOutputDirectory,
            createTrackingFiles,
            sailfDiff,
            scaleFish,
            executeNotificationHandler,
            sdSettings ?? new SailDiffSettings(),
            tags,
            args,
            providedBeforeTrackingFiles,
            timeStamp,
            testAssembliesAnchorTypes.Count == 0 ? new[] { GetType() } : testAssembliesAnchorTypes,
            registrationProviderAnchorTypes.Count == 0 ? new[] { GetType() } : registrationProviderAnchorTypes,
            disableOverheadEstimation,
            globalSampleSize,
            globalNumWarmupIterations,
            disableAnalysisGlobally,
            debg
        );
    }
}