using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;
using Sailfish.Logging;

namespace Sailfish.Contracts.Public.Models;

public interface IRunSettings
{
    IEnumerable<string> TestNames { get; }
    string LocalOutputDirectory { get; }
    bool RunSailDiff { get; }
    bool RunScaleFish { get; }
    bool CreateTrackingFiles { get; }
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
    bool DisableLogging { get; }
    ILogger? CustomLogger { get; }
    LogLevel MinimumLogLevel { get; }
    string GetRunSettingsTrackingDirectoryPath();
}