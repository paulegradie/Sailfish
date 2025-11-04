using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis;

using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish;

public class RunSettingsBuilder
{
    private readonly OrderedDictionary args = new();
    private readonly List<string> names = new();
    private readonly List<string> providedBeforeTrackingFiles = new();
    private readonly List<Type> registrationProviderAnchorTypes = new();
    private readonly OrderedDictionary tags = new();
    private readonly List<Type> testAssembliesAnchorTypes = new();
    private bool createTrackingFiles = true;
    private ILogger? customLogger;
    private bool disableAnalysisGlobally;
    private bool disableLogging;
    private bool disableOverheadEstimation;
    private int? globalNumWarmupIterations;
    private int? globalSampleSize;
    private LogLevel? level;
    private string? localOutputDir;
    private bool sailDiff;
    private bool scaleFish;
    private SailDiffSettings? sdSettings;
    private bool setDebug;
    private bool streamTrackingUpdates = true;
    private DateTime? timeStamp;


    // Global adaptive sampling overrides
    private bool? globalUseAdaptiveSampling;
    // Global outlier handling overrides
    private bool? globalUseConfigurableOutlierDetection;
    private OutlierStrategy? globalOutlierStrategy;

    private double? globalTargetCoefficientOfVariation;
    private int? globalMaximumSampleSize;

    public static RunSettingsBuilder CreateBuilder()
    {
        return new RunSettingsBuilder();
    }

    public RunSettingsBuilder WithMinimumLogLevel(LogLevel logLevel)
    {
        level = logLevel;
        return this;
    }

    public RunSettingsBuilder WithCustomLogger(ILogger logger)
    {
        customLogger = logger;
        return this;
    }

    /// <summary>
    ///     Disables logging to stdOut by setting the static global logger to an instance of the SilentLogger and setting the
    ///     disableLogging IRunSetting property to true.
    /// </summary>
    public RunSettingsBuilder DisableLogging()
    {
        disableLogging = true;
        return this;
    }

    /// <summary>
    ///     This method prevents the tracking data update notification from being emitted.
    ///     When this is used, the final tracking data will still be sent.
    ///     Consider using this when you want to ensure you capture test case results even when one test case may not finish in
    ///     a
    ///     reasonable amount of time.
    /// </summary>
    public RunSettingsBuilder DisableStreamingTrackingUpdates()
    {
        streamTrackingUpdates = false;
        return this;
    }

    /// <summary>
    ///     Provide a string array of class names to execute. This will run all test cases in a class decorated with the
    ///     SailfishAttribute.
    /// </summary>
    /// <param name="testNames"></param>
    public RunSettingsBuilder WithTestNames(params string[] testNames)
    {
        names.AddRange(testNames);
        return this;
    }

    /// <summary>
    ///     Specifies the name of an output directory to be created.
    /// </summary>
    /// <param name="localOutputDirectory"></param>
    /// <returns></returns>
    public RunSettingsBuilder WithLocalOutputDirectory(string localOutputDirectory)
    {
        localOutputDir = localOutputDirectory;
        return this;
    }

    /// <summary>
    ///     Configures whether or not to produce tracking files for the current run. Tracking files are used by SailDiff and
    ///     ScaleFish
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    public RunSettingsBuilder CreateTrackingFiles(bool track = true)
    {
        createTrackingFiles = track;
        return this;
    }

    /// <summary>
    ///     Configures whether or not to enable SailDiff for the current run.
    /// </summary>
    public RunSettingsBuilder WithSailDiff()
    {
        sailDiff = true;
        return this;
    }

    /// <summary>
    ///     Configures whether or not to enable SailDiff for the current run with a custom SailDiffSettings object.
    /// </summary>
    public RunSettingsBuilder WithSailDiff(SailDiffSettings settings)
    {
        sdSettings = settings;
        sailDiff = true;
        return this;
    }

    /// <summary>
    ///     Configures whether or not to enable ScaleFish for the current run.
    /// </summary>
    public RunSettingsBuilder WithScaleFish()
    {
        scaleFish = true;
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
        foreach (var runTag in tags) tags.Add(runTag.Key, runTag.Value);

        return this;
    }

    public RunSettingsBuilder WithArg(string key, string value)
    {
        args.Add(key, value);
        return this;
    }

    public RunSettingsBuilder WithArgs(OrderedDictionary runArgs)
    {
        foreach (var runArg in runArgs) args.Add(runArg.Key, runArg.Value);

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
        setDebug = debug;
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

    /// <summary>
    ///     Enables adaptive sampling globally and sets default convergence parameters.
    ///     Individual [Sailfish] attributes can still override these values per class.
    /// </summary>
    public RunSettingsBuilder WithGlobalAdaptiveSampling(double targetCoefficientOfVariation, int maximumSampleSize)
    {
        globalUseAdaptiveSampling = true;
        globalTargetCoefficientOfVariation = targetCoefficientOfVariation;
        globalMaximumSampleSize = maximumSampleSize;
        return this;
    }

        /// <summary>
        ///     Configures global outlier handling for the current run. When set, these values override per-class defaults.
        /// </summary>
        public RunSettingsBuilder WithGlobalOutlierHandling(bool useConfigurable, OutlierStrategy strategy)
        {
            globalUseConfigurableOutlierDetection = useConfigurable;
            globalOutlierStrategy = strategy;
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
            sailDiff,
            scaleFish,
            sdSettings ?? new SailDiffSettings(),
            tags,
            args,
            providedBeforeTrackingFiles,
            testAssembliesAnchorTypes.Count == 0 ? new[] { GetType() } : testAssembliesAnchorTypes,
            registrationProviderAnchorTypes.Count == 0 ? new[] { GetType() } : registrationProviderAnchorTypes,
            customLogger,
            disableOverheadEstimation,
            timeStamp,
            globalSampleSize,
            globalNumWarmupIterations,
            disableAnalysisGlobally,
            streamTrackingUpdates,
            disableLogging,
            level ?? LogLevel.Verbose,
            setDebug,
            globalUseAdaptiveSampling,
            globalTargetCoefficientOfVariation,
            globalMaximumSampleSize,
            globalUseConfigurableOutlierDetection,
            globalOutlierStrategy);
    }
}