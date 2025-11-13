using System;
using System.Collections.Generic;
using System.IO;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis;

using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish;

internal class RunSettings(
    IEnumerable<string> testNames,
    string localOutputDirectory,
    bool createTrackingFiles,
    bool useSailDiff,
    bool useScaleFish,
    SailDiffSettings sailDiffSettings,
    OrderedDictionary tags,
    OrderedDictionary args,
    IEnumerable<string> providedBeforeTrackingFiles,
    IEnumerable<Type> testLocationAnchors,
    IEnumerable<Type> registrationProviderAnchors,
    ILogger? customLogger,
    bool disableOverheadEstimation = false,
    DateTime? timeStamp = null,
    int? sampleSizeOverride = null,
    int? numWarmupIterationsOverride = null,
    bool disableAnalysisGlobally = false,
    bool streamTrackingUpdates = true,
    bool disableLogging = false,
    LogLevel minimumLogLevel = LogLevel.Verbose,
    bool debug = false,
    bool? globalUseAdaptiveSampling = null,
    double? globalTargetCoefficientOfVariation = null,
    int? globalMaximumSampleSize = null,
    bool? globalUseConfigurableOutlierDetection = null,
    OutlierStrategy? globalOutlierStrategy = null,
    bool enableEnvironmentHealthCheck = true,
    bool timerCalibration = true,
    int? seed = null) : IRunSettings
{
    public IEnumerable<string> TestNames { get; } = testNames;
    public string LocalOutputDirectory { get; } = localOutputDirectory;
    public bool CreateTrackingFiles { get; } = createTrackingFiles;
    public bool RunSailDiff { get; } = useSailDiff;
    public bool RunScaleFish { get; } = useScaleFish;
    public SailDiffSettings SailDiffSettings { get; } = sailDiffSettings;
    public IEnumerable<Type> TestLocationAnchors { get; } = testLocationAnchors;
    public IEnumerable<Type> RegistrationProviderAnchors { get; } = registrationProviderAnchors;
    public OrderedDictionary Tags { get; } = tags;
    public OrderedDictionary Args { get; } = args;
    public int? Seed { get; } = seed;
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; } = providedBeforeTrackingFiles;
    public DateTime TimeStamp { get; } = timeStamp ?? DateTime.Now.ToUniversalTime();
    public bool DisableOverheadEstimation { get; } = disableOverheadEstimation;
    public bool DisableAnalysisGlobally { get; } = disableAnalysisGlobally;
    public int? SampleSizeOverride { get; } = sampleSizeOverride;
    public int? NumWarmupIterationsOverride { get; } = numWarmupIterationsOverride;
    public bool Debug { get; } = debug;
    public bool StreamTrackingUpdates { get; } = streamTrackingUpdates;
    public bool? GlobalUseAdaptiveSampling { get; } = globalUseAdaptiveSampling;
    public double? GlobalTargetCoefficientOfVariation { get; } = globalTargetCoefficientOfVariation;
    public int? GlobalMaximumSampleSize { get; } = globalMaximumSampleSize;
    public bool? GlobalUseConfigurableOutlierDetection { get; } = globalUseConfigurableOutlierDetection;
    public OutlierStrategy? GlobalOutlierStrategy { get; } = globalOutlierStrategy;


    public bool DisableLogging { get; } = disableLogging;
    public ILogger? CustomLogger { get; } = customLogger;
    public bool EnableEnvironmentHealthCheck { get; } = enableEnvironmentHealthCheck;
    public bool TimerCalibration { get; } = timerCalibration;

    public LogLevel MinimumLogLevel { get; } = minimumLogLevel;

    public string GetRunSettingsTrackingDirectoryPath()
    {
        var trackingDirectoryPath = Path.Join(LocalOutputDirectory, DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory);
        if (!Directory.Exists(trackingDirectoryPath)) Directory.CreateDirectory(trackingDirectoryPath);
        return trackingDirectoryPath;
    }
}