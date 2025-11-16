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

internal class RunSettings : IRunSettings
{
    public RunSettings(IEnumerable<string> testNames,
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
        int? seed = null)
    {
        TestNames = testNames;
        LocalOutputDirectory = localOutputDirectory;
        CreateTrackingFiles = createTrackingFiles;
        RunSailDiff = useSailDiff;
        RunScaleFish = useScaleFish;
        SailDiffSettings = sailDiffSettings;
        TestLocationAnchors = testLocationAnchors;
        RegistrationProviderAnchors = registrationProviderAnchors;
        Tags = tags;
        Args = args;
        Seed = seed;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        TimeStamp = timeStamp ?? DateTime.Now.ToUniversalTime();
        DisableOverheadEstimation = disableOverheadEstimation;
        DisableAnalysisGlobally = disableAnalysisGlobally;
        SampleSizeOverride = sampleSizeOverride;
        NumWarmupIterationsOverride = numWarmupIterationsOverride;
        Debug = debug;
        StreamTrackingUpdates = streamTrackingUpdates;
        GlobalUseAdaptiveSampling = globalUseAdaptiveSampling;
        GlobalTargetCoefficientOfVariation = globalTargetCoefficientOfVariation;
        GlobalMaximumSampleSize = globalMaximumSampleSize;
        GlobalUseConfigurableOutlierDetection = globalUseConfigurableOutlierDetection;
        GlobalOutlierStrategy = globalOutlierStrategy;
        DisableLogging = disableLogging;
        CustomLogger = customLogger;
        EnableEnvironmentHealthCheck = enableEnvironmentHealthCheck;
        TimerCalibration = timerCalibration;
        MinimumLogLevel = minimumLogLevel;
    }

    public IEnumerable<string> TestNames { get; }
    public string LocalOutputDirectory { get; }
    public bool CreateTrackingFiles { get; }
    public bool RunSailDiff { get; }
    public bool RunScaleFish { get; }
    public SailDiffSettings SailDiffSettings { get; }
    public IEnumerable<Type> TestLocationAnchors { get; }
    public IEnumerable<Type> RegistrationProviderAnchors { get; }
    public OrderedDictionary Tags { get; }
    public OrderedDictionary Args { get; }
    public int? Seed { get; }
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    public DateTime TimeStamp { get; }
    public bool DisableOverheadEstimation { get; }
    public bool DisableAnalysisGlobally { get; }
    public int? SampleSizeOverride { get; }
    public int? NumWarmupIterationsOverride { get; }
    public bool Debug { get; }
    public bool StreamTrackingUpdates { get; }
    public bool? GlobalUseAdaptiveSampling { get; }
    public double? GlobalTargetCoefficientOfVariation { get; }
    public int? GlobalMaximumSampleSize { get; }
    public bool? GlobalUseConfigurableOutlierDetection { get; }
    public OutlierStrategy? GlobalOutlierStrategy { get; }


    public bool DisableLogging { get; }
    public ILogger? CustomLogger { get; }
    public bool EnableEnvironmentHealthCheck { get; }
    public bool TimerCalibration { get; }

    public LogLevel MinimumLogLevel { get; }

    public string GetRunSettingsTrackingDirectoryPath()
    {
        var trackingDirectoryPath = Path.Join(LocalOutputDirectory, DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory);
        if (!Directory.Exists(trackingDirectoryPath)) Directory.CreateDirectory(trackingDirectoryPath);
        return trackingDirectoryPath;
    }
}