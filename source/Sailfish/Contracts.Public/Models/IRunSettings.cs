using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.Ai;
using Sailfish.Trawl;

using Sailfish.Extensions.Types;
using Sailfish.Logging;

namespace Sailfish.Contracts.Public.Models;

public interface IRunSettings
{
    IEnumerable<string> TestNames { get; }
    string LocalOutputDirectory { get; }
    bool RunSailDiff { get; }
    bool RunScaleFish { get; }
    bool RunAiAnalysis { get; }
    bool CreateTrackingFiles { get; }
    SailDiffSettings SailDiffSettings { get; }
    ScaleFishSettings ScaleFishSettings { get; }
    AiAnalysisSettings AiAnalysisSettings { get; }

    /// <summary>
    ///     Run-wide Trawl (load testing) settings and overrides applied to every <c>[Trawl]</c> load
    ///     scenario in the run (kill switch, virtual-user / duration / warmup overrides, the regression
    ///     gate, and run retention). See <see cref="Sailfish.Trawl.TrawlSettings" />.
    /// </summary>
    TrawlSettings TrawlSettings { get; }
    IEnumerable<Type> TestLocationAnchors { get; }
    IEnumerable<Type> RegistrationProviderAnchors { get; }
    OrderedDictionary Tags { get; }
    OrderedDictionary Args { get; }
    // Optional deterministic randomization seed for reproducible ordering
    int? Seed { get; }
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

    // Environment health check toggle (default: true)
    bool EnableEnvironmentHealthCheck { get; }

    // Timer calibration toggle (default: true)
    bool TimerCalibration { get; }

    // Inline Unicode distribution plots in IDE / Markdown output (default: true)
    bool EnableDistributionPlots { get; }

    // Which inline distribution plot to render — histogram (default) or box-and-whisker
    Sailfish.Presentation.DistributionPlotStyle DistributionPlotStyle { get; }

    // Emit a standalone SVG distribution HTML report alongside the run output (default: false)
    bool EmitDistributionHtmlReport { get; }

    // Global adaptive sampling overrides (null = no override)
    bool? GlobalUseAdaptiveSampling { get; }
    double? GlobalTargetCoefficientOfVariation { get; }
    double? GlobalMaxConfidenceIntervalWidth { get; }
    int? GlobalMinimumSampleSize { get; }
    int? GlobalMaximumSampleSize { get; }

        // Global outlier handling overrides (null = no override)
        bool? GlobalUseConfigurableOutlierDetection { get; }
        OutlierStrategy? GlobalOutlierStrategy { get; }

    string GetRunSettingsTrackingDirectoryPath();
}
