using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;

namespace Sailfish;

internal class RunSettings : IRunSettings
{
    public IEnumerable<string> TestNames { get; }
    public string? LocalOutputDirectory { get; }
    public bool CreateTrackingFiles { get; }
    public bool RunSailDiff { get; }
    public bool RunScalefish { get; set; }
    public bool Notify { get; set; }
    public SailDiffSettings Settings { get; }
    public IEnumerable<Type> TestLocationAnchors { get; }
    public IEnumerable<Type> RegistrationProviderAnchors { get; }
    public OrderedDictionary Tags { get; set; } = new();
    public OrderedDictionary Args { get; } = new();
    public IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    public DateTime? TimeStamp { get; }
    public bool DisableOverheadEstimation { get; }
    public bool DisableAnalysisGlobally { get; set; }
    public int? SampleSizeOverride { get; set; }
    public int? NumWarmupIterationsOverride { get; set; }
    public bool Debug { get; set; }

    public RunSettings(
        IEnumerable<string> testNames,
        string localOutputDirectory,
        bool createTrackingFiles,
        bool useSailDiff,
        bool useScaleFish,
        bool notify,
        SailDiffSettings settings,
        OrderedDictionary tags,
        OrderedDictionary args,
        IEnumerable<string> providedBeforeTrackingFiles,
        DateTime? timeStamp,
        IEnumerable<Type> testLocationAnchors,
        IEnumerable<Type> registrationProviderAnchors)
    {
        TestNames = testNames;
        LocalOutputDirectory = localOutputDirectory;
        CreateTrackingFiles = createTrackingFiles;
        RunSailDiff = useSailDiff;
        RunScalefish = useScaleFish;
        Settings = settings;
        Tags = tags;
        Args = args;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        TimeStamp = timeStamp;
        Debug = false;
        Notify = notify;
        TestLocationAnchors = testLocationAnchors;
        RegistrationProviderAnchors = registrationProviderAnchors;
        DisableOverheadEstimation = false;
        DisableAnalysisGlobally = false;
    }

    public RunSettings(
        IEnumerable<string> testNames,
        string localOutputDirectory,
        bool createTrackingFiles,
        bool useSailDiff,
        bool useScaleFish,
        bool notify,
        SailDiffSettings settings,
        OrderedDictionary tags,
        OrderedDictionary args,
        IEnumerable<string> providedBeforeTrackingFiles,
        DateTime? timeStamp,
        IEnumerable<Type> testLocationAnchors,
        IEnumerable<Type> registrationProviderAnchors,
        bool disableOverheadEstimation,
        int? sampleSizeOverride = null,
        int? numWarmupIterationsOverride = null,
        bool disableAnalysisGlobally = false,
        bool debug = false)
    {
        TestNames = testNames;
        LocalOutputDirectory = localOutputDirectory;
        CreateTrackingFiles = createTrackingFiles;
        RunSailDiff = useSailDiff;
        RunScalefish = useScaleFish;
        Settings = settings;
        Tags = tags;
        Args = args;
        ProvidedBeforeTrackingFiles = providedBeforeTrackingFiles;
        TimeStamp = timeStamp;
        Debug = debug;
        Notify = notify;
        TestLocationAnchors = testLocationAnchors;
        RegistrationProviderAnchors = registrationProviderAnchors;
        DisableOverheadEstimation = disableOverheadEstimation;
        DisableAnalysisGlobally = disableAnalysisGlobally;
        SampleSizeOverride = sampleSizeOverride;
        NumWarmupIterationsOverride = numWarmupIterationsOverride;
    }

    // default
    public RunSettings()
    {
        TestNames = Array.Empty<string>();
        LocalOutputDirectory = null;
        Settings = new SailDiffSettings();
        TestLocationAnchors = new[] { GetType() };
        RegistrationProviderAnchors = new[] { GetType() };
        ProvidedBeforeTrackingFiles = Array.Empty<string>();
        RunScalefish = true;
        RunSailDiff = true;
    }
}