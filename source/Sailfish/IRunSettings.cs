using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;

namespace Sailfish;

public interface IRunSettings
{
    IEnumerable<string> TestNames { get; }
    string LocalOutputDirectory { get; }
    bool RunSailDiff { get; }
    bool RunScalefish { get; }
    bool CreateTrackingFiles { get; }
    bool Notify { get; }
    SailDiffSettings SailDiffSettings { get; }
    IEnumerable<Type> TestLocationAnchors { get; }
    IEnumerable<Type> RegistrationProviderAnchors { get; }
    OrderedDictionary Tags { get; }
    OrderedDictionary Args { get; }
    IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    DateTime TimeStamp { get; }
    bool DisableOverheadEstimation { get; }
    public bool DisableAnalysisGlobally { get; }
    public int? SampleSizeOverride { get; }
    public int? NumWarmupIterationsOverride { get; }
    bool Debug { get; }
    bool StreamTrackingUpdates { get; }

    string GetRunSettingsTrackingDirectoryPath();
}