using System;
using System.Collections.Generic;
using Sailfish.Analysis.Saildiff;
using Sailfish.Extensions.Types;

namespace Sailfish;

public interface IRunSettings
{
    IEnumerable<string> TestNames { get; }
    string? LocalOutputDirectory { get; }
    bool RunSailDiff { get; }
    bool RunScalefish { get; }
    bool CreateTrackingFiles { get; }
    bool Notify { get; }
    TestSettings Settings { get; }
    IEnumerable<Type> TestLocationAnchors { get; }
    IEnumerable<Type> RegistrationProviderAnchors { get; }
    OrderedDictionary Tags { get; }
    OrderedDictionary Args { get; }
    IEnumerable<string> ProvidedBeforeTrackingFiles { get; }
    DateTime? TimeStamp { get; }
    bool DisableOverheadEstimation { get; }
    public bool DisableAnalysisGlobally { get; }
    bool Debug { get; set; }
}