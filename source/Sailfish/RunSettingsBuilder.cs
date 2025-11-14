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
    private readonly OrderedDictionary _args = new();
    private readonly List<string> _names = new();
    private readonly List<string> _providedBeforeTrackingFiles = new();
    private readonly List<Type> _registrationProviderAnchorTypes = new();
    private readonly OrderedDictionary _tags = new();
    private readonly List<Type> _testAssembliesAnchorTypes = new();
    private bool _createTrackingFiles = true;
    private ILogger? _customLogger;
    private bool _disableAnalysisGlobally;
    private bool _disableLogging;
    private bool _disableOverheadEstimation;
    private int? _globalNumWarmupIterations;
    private int? _globalSampleSize;
    private LogLevel? _level;
    private string? _localOutputDir;
    private bool _sailDiff;
    private bool _scaleFish;
    private SailDiffSettings? _sdSettings;
    private bool _setDebug;
    private bool _streamTrackingUpdates = true;
    private DateTime? _timeStamp;

    private bool _enableEnvironmentHealthCheck = true;
    private bool _timerCalibration = true;


    // Optional deterministic randomization seed for reproducible ordering
    private int? _seed;

    // Global adaptive sampling overrides
    private bool? _globalUseAdaptiveSampling;
    // Global outlier handling overrides
    private bool? _globalUseConfigurableOutlierDetection;
    private OutlierStrategy? _globalOutlierStrategy;

    private double? _globalTargetCoefficientOfVariation;
    private int? _globalMaximumSampleSize;

    public static RunSettingsBuilder CreateBuilder()
    {
        return new RunSettingsBuilder();
    }

    public RunSettingsBuilder WithMinimumLogLevel(LogLevel logLevel)
    {
        _level = logLevel;
        return this;
    }

    public RunSettingsBuilder WithCustomLogger(ILogger logger)
    {
        _customLogger = logger;
        return this;
    }

    /// <summary>
    ///     Disables logging to stdOut by setting the static global logger to an instance of the SilentLogger and setting the
    ///     disableLogging IRunSetting property to true.
    /// </summary>
    public RunSettingsBuilder DisableLogging()
    {
        _disableLogging = true;
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
        _streamTrackingUpdates = false;
        return this;
    }

    /// <summary>
    ///     Enables or disables the environment health check for this run (default: true).
    /// </summary>
    public RunSettingsBuilder WithEnvironmentHealthCheck(bool enable = true)
    {
        _enableEnvironmentHealthCheck = enable;
        return this;
    }
    /// <summary>
    ///     Enables or disables timer calibration for this run (default: true).
    /// </summary>
    public RunSettingsBuilder WithTimerCalibration(bool enable = true)
    {
        _timerCalibration = enable;
        return this;
    }


    /// <summary>
    ///     Provide a string array of class names to execute. This will run all test cases in a class decorated with the
    ///     SailfishAttribute.
    /// </summary>
    /// <param name="testNames"></param>
    public RunSettingsBuilder WithTestNames(params string[] testNames)
    {
        _names.AddRange(testNames);
        return this;
    }

    /// <summary>
    ///     Specifies the name of an output directory to be created.
    /// </summary>
    /// <param name="localOutputDirectory"></param>
    /// <returns></returns>
    public RunSettingsBuilder WithLocalOutputDirectory(string localOutputDirectory)
    {
        _localOutputDir = localOutputDirectory;
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
        _createTrackingFiles = track;
        return this;
    }

    /// <summary>
    ///     Configures whether or not to enable SailDiff for the current run.
    /// </summary>
    public RunSettingsBuilder WithSailDiff()
    {
        _sailDiff = true;
        return this;
    }

    /// <summary>
    ///     Configures whether or not to enable SailDiff for the current run with a custom SailDiffSettings object.
    /// </summary>
    public RunSettingsBuilder WithSailDiff(SailDiffSettings settings)
    {
        _sdSettings = settings;
        _sailDiff = true;
        return this;
    }

    /// <summary>
    ///     Configures whether or not to enable ScaleFish for the current run.
    /// </summary>
    public RunSettingsBuilder WithScaleFish()
    {
        _scaleFish = true;
        return this;
    }

    public RunSettingsBuilder TestsFromAssembliesContaining(params Type[] anchorTypes)
    {
        _testAssembliesAnchorTypes.AddRange(anchorTypes);
        return this;
    }

    public RunSettingsBuilder ProvidersFromAssembliesContaining(params Type[] anchorTypes)
    {
        _registrationProviderAnchorTypes.AddRange(anchorTypes);
        return this;
    }

    public RunSettingsBuilder WithTag(string key, string value)
    {
        _tags.Add(key, value);
        return this;
    }

    public RunSettingsBuilder WithTags(OrderedDictionary tags)
    {
        foreach (var runTag in tags) tags.Add(runTag.Key, runTag.Value);

        return this;
    }

    public RunSettingsBuilder WithArg(string key, string value)
    {
        _args.Add(key, value);
        return this;
    }

    public RunSettingsBuilder WithArgs(OrderedDictionary runArgs)
    {
        foreach (var runArg in runArgs) _args.Add(runArg.Key, runArg.Value);

        return this;
    }

    public RunSettingsBuilder WithProvidedBeforeTrackingFile(string trackingFile)
    {
        _providedBeforeTrackingFiles.Add(trackingFile);
        return this;
    }

    public RunSettingsBuilder WithProvidedBeforeTrackingFiles(IEnumerable<string> trackingFiles)
    {
        _providedBeforeTrackingFiles.AddRange(trackingFiles);
        return this;
    }

    public RunSettingsBuilder WithTimeStamp(DateTime dateTime)
    {
        _timeStamp = dateTime;
        return this;
    }

    public RunSettingsBuilder InDebugMode(bool debug = false)
    {
        _setDebug = debug;
        return this;
    }

    /// <summary>
    ///     Sets a deterministic randomization seed for reproducible ordering of tests, methods, and property sets.
    /// </summary>
    public RunSettingsBuilder WithSeed(int seed)
    {
        this._seed = seed;
        return this;
    }

    public RunSettingsBuilder DisableOverheadEstimation()
    {
        _disableOverheadEstimation = true;
        return this;
    }

    public RunSettingsBuilder WithAnalysisDisabledGlobally()
    {
        _disableAnalysisGlobally = true;
        return this;
    }

    public RunSettingsBuilder WithGlobalSampleSize(int sampleSize)
    {
        _globalSampleSize = Math.Max(sampleSize, 1);
        return this;
    }

    /// <summary>
    ///     Enables adaptive sampling globally and sets default convergence parameters.
    ///     Individual [Sailfish] attributes can still override these values per class.
    /// </summary>
    public RunSettingsBuilder WithGlobalAdaptiveSampling(double targetCoefficientOfVariation, int maximumSampleSize)
    {
        _globalUseAdaptiveSampling = true;
        _globalTargetCoefficientOfVariation = targetCoefficientOfVariation;
        _globalMaximumSampleSize = maximumSampleSize;
        return this;
    }

        /// <summary>
        ///     Configures global outlier handling for the current run. When set, these values override per-class defaults.
        /// </summary>
        public RunSettingsBuilder WithGlobalOutlierHandling(bool useConfigurable, OutlierStrategy strategy)
        {
            _globalUseConfigurableOutlierDetection = useConfigurable;
            _globalOutlierStrategy = strategy;
            return this;
        }



    public RunSettingsBuilder WithGlobalNumWarmupIterations(int numIterations)
    {
        _globalNumWarmupIterations = Math.Max(numIterations, 1);
        return this;
    }

    public IRunSettings Build()
    {
        return new RunSettings(
            _names,
            _localOutputDir ?? DefaultFileSettings.DefaultOutputDirectory,
            _createTrackingFiles,
            _sailDiff,
            _scaleFish,
            _sdSettings ?? new SailDiffSettings(),
            _tags,
            _args,
            _providedBeforeTrackingFiles,
            _testAssembliesAnchorTypes.Count == 0 ? new[] { GetType() } : _testAssembliesAnchorTypes,
            _registrationProviderAnchorTypes.Count == 0 ? new[] { GetType() } : _registrationProviderAnchorTypes,
            _customLogger,
            _disableOverheadEstimation,
            _timeStamp,
            _globalSampleSize,
            _globalNumWarmupIterations,
            _disableAnalysisGlobally,
            _streamTrackingUpdates,
            _disableLogging,
            _level ?? LogLevel.Verbose,
            _setDebug,
            _globalUseAdaptiveSampling,
            _globalTargetCoefficientOfVariation,
            _globalMaximumSampleSize,
            _globalUseConfigurableOutlierDetection,
            _globalOutlierStrategy,
            _enableEnvironmentHealthCheck,
            _timerCalibration,
            seed: _seed);
    }
}