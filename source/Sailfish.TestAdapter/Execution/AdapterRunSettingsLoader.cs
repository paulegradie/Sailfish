using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestSettingsParser;
using SailDiffSettings = Sailfish.Analysis.SailDiff.SailDiffSettings;
using RuntimeScaleFishSettings = Sailfish.Analysis.ScaleFish.ScaleFishSettings;
using RuntimeTrawlSettings = Sailfish.Trawl.TrawlSettings;
using CoreAiAnalysisSettings = Sailfish.Analysis.Ai.AiAnalysisSettings;
using TrackingFiles = Sailfish.Analysis.SailDiff.TrackingFiles;
using DefaultFileSettings = Sailfish.Presentation.DefaultFileSettings;

namespace Sailfish.TestAdapter.Execution;

public static class AdapterRunSettingsLoader
{
    /// <summary>
    ///     Loads the adapter run settings from <c>.sailfish.json</c>, falling back to defaults when none is found.
    ///     Prefer the overload that takes the test assembly location and a message logger so discovery can also
    ///     search the assembly's output directory and so the loaded-file / not-found outcome is observable.
    /// </summary>
    public static IRunSettings RetrieveAndLoadAdapterRunSettings()
    {
        return RetrieveAndLoadAdapterRunSettings(null, null);
    }

    /// <summary>
    ///     Loads the adapter run settings from <c>.sailfish.json</c>.
    /// </summary>
    /// <param name="testAssemblyLocation">
    ///     Path to the test assembly (e.g. the test DLL). When provided, discovery also searches the assembly's
    ///     own directory upward — not only upward from the working directory — so the file is found regardless of
    ///     the test host's working directory (and the <c>&lt;None CopyToOutputDirectory&gt;</c> convention, where
    ///     <c>.sailfish.json</c> sits beside the test DLL, works).
    /// </param>
    /// <param name="messageLogger">
    ///     Optional VSTest message logger. When provided, the loader logs which <c>.sailfish.json</c> was loaded
    ///     (info) or warns (once) that none was found and defaults are in effect — so AI analysis being off is
    ///     never a silent surprise.
    /// </param>
    public static IRunSettings RetrieveAndLoadAdapterRunSettings(string? testAssemblyLocation, IMessageLogger? messageLogger)
    {
        var parsedSettings = ParseSettings(testAssemblyLocation, messageLogger);

        if (parsedSettings.GlobalSettings.DisableEverything) throw new SailfishException("Everything is disabled!");

        var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
        if (!string.IsNullOrEmpty(parsedSettings.GlobalSettings.ResultsDirectory))
            runSettingsBuilder = runSettingsBuilder.WithLocalOutputDirectory(parsedSettings.GlobalSettings.ResultsDirectory);

        if (parsedSettings.SailDiffSettings.Disabled) runSettingsBuilder = runSettingsBuilder.WithAnalysisDisabledGlobally();

        if (parsedSettings.SailfishSettings.DisableOverheadEstimation) runSettingsBuilder = runSettingsBuilder.DisableOverheadEstimation();

        if (parsedSettings.SailfishSettings.SampleSizeOverride is not null)
            runSettingsBuilder = runSettingsBuilder.WithGlobalSampleSize(parsedSettings.SailfishSettings.SampleSizeOverride.Value);

        if (parsedSettings.SailfishSettings.NumWarmupIterationsOverride is not null)
            runSettingsBuilder = runSettingsBuilder.WithGlobalNumWarmupIterations(parsedSettings.SailfishSettings.NumWarmupIterationsOverride.Value);

        if (parsedSettings.SailfishSettings.EnableEnvironmentHealthCheck is not null)
            runSettingsBuilder = runSettingsBuilder.WithEnvironmentHealthCheck(parsedSettings.SailfishSettings.EnableEnvironmentHealthCheck.Value);

        if (parsedSettings.SailfishSettings.TimerCalibration is not null)
            runSettingsBuilder = runSettingsBuilder.WithTimerCalibration(parsedSettings.SailfishSettings.TimerCalibration.Value);

        if (parsedSettings.GlobalSettings.EnableDistributionPlots is not null)
            runSettingsBuilder = runSettingsBuilder.WithDistributionPlots(parsedSettings.GlobalSettings.EnableDistributionPlots.Value);

        // Canonical home is GlobalSettings; also accept it under SailDiffSettings as a convenience.
        var plotStyleRaw = parsedSettings.GlobalSettings.DistributionPlotStyle ?? parsedSettings.SailDiffSettings.DistributionPlotStyle;
        if (!string.IsNullOrWhiteSpace(plotStyleRaw)
            && System.Enum.TryParse<Sailfish.Presentation.DistributionPlotStyle>(plotStyleRaw, ignoreCase: true, out var plotStyle))
            runSettingsBuilder = runSettingsBuilder.WithDistributionPlotStyle(plotStyle);

        if (parsedSettings.GlobalSettings.EmitDistributionHtmlReport is not null)
            runSettingsBuilder = runSettingsBuilder.WithDistributionHtmlReport(parsedSettings.GlobalSettings.EmitDistributionHtmlReport.Value);

        // Skipper AI analysis is opt-in via .sailfish.json. A Skipper transport must also be registered through
        // IRegisterSailfishServices (services.AddSkipperTransport<T>()); without one, enabling this is a no-op
        // (the adapter warns about it after the run — see TestExecutor — rather than failing silently).
        if (parsedSettings.AiAnalysisSettings is { Enabled: true } ai)
            runSettingsBuilder = runSettingsBuilder.WithAiAnalysis(new CoreAiAnalysisSettings(
                writeReviewArtifact: ai.WriteReviewArtifact ?? true,
                emitConsoleSummary: ai.EmitConsoleSummary ?? true,
                useResponseCache: ai.UseResponseCache ?? true));

        // Historical (run-vs-run) comparison is explicit by default: SailDiff compares against an earlier run
        // only when the user names the 'before' file(s). The opt-in AutoCompareToPreviousRun flag is the
        // deliberate way to get the "run twice → SailDiff" workflow without naming a file by hand.
        ConfigureHistoricalComparison(ref runSettingsBuilder, parsedSettings, messageLogger);

        var testSettings = MapToTestSettings(parsedSettings);
        var scaleFishSettings = MapToScaleFishSettings(parsedSettings);
        var trawlSettings = MapToTrawlSettings(parsedSettings);
        var runSettings = runSettingsBuilder
            .CreateTrackingFiles()
            .WithSailDiff(testSettings)
            .WithScaleFish(scaleFishSettings)
            .WithTrawl(trawlSettings)
            .Build();
        return runSettings;
    }

    /// <summary>
    ///     Wires up the 'before' tracking file(s) for a historical comparison. An explicit
    ///     <c>ProvidedBeforeTrackingFiles</c> always wins. Otherwise, when <c>AutoCompareToPreviousRun</c> is
    ///     enabled, the most recent prior tracking file is resolved here — <em>before</em> this run writes its
    ///     own file, so "most recent" is genuinely the previous run.
    /// </summary>
    private static void ConfigureHistoricalComparison(
        ref RunSettingsBuilder runSettingsBuilder,
        SettingsConfiguration parsedSettings,
        IMessageLogger? messageLogger)
    {
        var providedBeforeTrackingFiles = parsedSettings.SailDiffSettings.ProvidedBeforeTrackingFiles;
        if (providedBeforeTrackingFiles is { Length: > 0 })
        {
            runSettingsBuilder = runSettingsBuilder.WithProvidedBeforeTrackingFiles(providedBeforeTrackingFiles);
            return;
        }

        if (parsedSettings.SailDiffSettings.AutoCompareToPreviousRun != true) return;

        var outputDir = string.IsNullOrEmpty(parsedSettings.GlobalSettings.ResultsDirectory)
            ? DefaultFileSettings.DefaultOutputDirectory
            : parsedSettings.GlobalSettings.ResultsDirectory;
        var trackingDir = Path.Combine(outputDir, TrackingFiles.DefaultTrackingDirectoryName);
        var previousRun = TrackingFiles.MostRecentIn(trackingDir);

        if (previousRun is not null)
        {
            runSettingsBuilder = runSettingsBuilder.WithProvidedBeforeTrackingFile(previousRun);
            Log(messageLogger, TestMessageLevel.Informational,
                $"Sailfish: AutoCompareToPreviousRun is on — comparing this run against the most recent prior tracking file: {previousRun}");
        }
        else
        {
            Log(messageLogger, TestMessageLevel.Informational,
                "Sailfish: AutoCompareToPreviousRun is on but no prior tracking file was found, so there is no run-vs-run comparison this run (expected on the first run).");
        }
    }

    private static RuntimeScaleFishSettings MapToScaleFishSettings(SettingsConfiguration settingsConfiguration)
    {
        var mapped = new RuntimeScaleFishSettings();
        var parsed = settingsConfiguration.ScaleFishSettings;
        // `SettingsConfiguration.ScaleFishSettings` is initialized inline to a default instance, but a
        // user can explicitly set the JSON property to null. Return defaults in that case rather than NRE.
        if (parsed is null) return mapped;
        if (parsed.EnableBootstrap is not null) mapped.EnableBootstrap = parsed.EnableBootstrap.Value;
        if (parsed.BootstrapIterations is not null) mapped.BootstrapIterations = parsed.BootstrapIterations.Value;
        if (parsed.EnableParallelBootstrap is not null) mapped.EnableParallelBootstrap = parsed.EnableParallelBootstrap.Value;
        if (parsed.EnableContinuousExponent is not null) mapped.EnableContinuousExponent = parsed.EnableContinuousExponent.Value;
        if (parsed.DistinguishabilityDelta is not null) mapped.DistinguishabilityDelta = parsed.DistinguishabilityDelta.Value;
        if (parsed.EnableCrossValidation is not null) mapped.EnableCrossValidation = parsed.EnableCrossValidation.Value;
        if (parsed.EnableTailPercentileFits is not null) mapped.EnableTailPercentileFits = parsed.EnableTailPercentileFits.Value;
        if (parsed.TailPercentiles is not null && parsed.TailPercentiles.Length > 0) mapped.TailPercentiles = parsed.TailPercentiles;
        if (parsed.EnableTrendTracking is not null) mapped.EnableTrendTracking = parsed.EnableTrendTracking.Value;
        if (parsed.EmitHtmlReport is not null) mapped.EmitHtmlReport = parsed.EmitHtmlReport.Value;
        return mapped;
    }

    private static RuntimeTrawlSettings MapToTrawlSettings(SettingsConfiguration settingsConfiguration)
    {
        var mapped = new RuntimeTrawlSettings();
        var parsed = settingsConfiguration.TrawlSettings;
        // `SettingsConfiguration.TrawlSettings` is initialized inline to a default instance, but a user can
        // explicitly set the JSON property to null. Return defaults in that case rather than NRE.
        if (parsed is null) return mapped;
        if (parsed.Disabled is not null) mapped.Disabled = parsed.Disabled.Value;
        if (parsed.VirtualUsersOverride is not null) mapped.VirtualUsersOverride = parsed.VirtualUsersOverride.Value;
        if (parsed.MaxDurationSecondsOverride is not null) mapped.MaxDurationSecondsOverride = parsed.MaxDurationSecondsOverride.Value;
        if (parsed.WarmupSecondsOverride is not null) mapped.WarmupSecondsOverride = parsed.WarmupSecondsOverride.Value;
        if (parsed.FailOnRegression is not null) mapped.FailOnRegression = parsed.FailOnRegression.Value;
        if (parsed.MaxRetainedRunsPerScenario is not null) mapped.MaxRetainedRunsPerScenario = parsed.MaxRetainedRunsPerScenario.Value;
        return mapped;
    }

    private static SailDiffSettings MapToTestSettings(SettingsConfiguration settingsConfiguration)
    {
        var mappedSettings = new SailDiffSettings();
        if (settingsConfiguration?.SailDiffSettings.TestType is not null) mappedSettings.SetTestType(settingsConfiguration.SailDiffSettings.TestType);

        if (settingsConfiguration?.GlobalSettings.DisableOutlierDetection is true) mappedSettings.DisableOutlierDetection();

        if (settingsConfiguration?.SailDiffSettings.Alpha is not null) mappedSettings.SetAlpha(settingsConfiguration.SailDiffSettings.Alpha);

        if (settingsConfiguration?.GlobalSettings.Round is not null) mappedSettings.SetRound(settingsConfiguration.GlobalSettings.Round);

        if (settingsConfiguration?.SailDiffSettings.EquivalenceMarginPercent is not null)
            mappedSettings.SetEquivalenceMarginPercent(settingsConfiguration.SailDiffSettings.EquivalenceMarginPercent);

        return mappedSettings;
    }

    private static SettingsConfiguration ParseSettings(string? testAssemblyLocation, IMessageLogger? messageLogger)
    {
        var settingsFile = LocateSettingsFile(testAssemblyLocation);
        if (settingsFile is null)
        {
            // No .sailfish.json anywhere up the tree from the working directory or the test assembly — that is
            // an expected, supported configuration, but it is the silent path that makes AI analysis appear to
            // do nothing. Warn once (not debug) so it is observable, then fall back to defaults.
            Log(messageLogger, TestMessageLevel.Warning,
                "Sailfish: no .sailfish.json was found (searched upward from the working directory" +
                (string.IsNullOrEmpty(testAssemblyLocation) ? "" : " and the test assembly directory") +
                "); using built-in defaults. Skipper AI analysis stays OFF unless a .sailfish.json sets " +
                "AiAnalysisSettings.Enabled = true.");
            return new SettingsConfiguration();
        }

        Log(messageLogger, TestMessageLevel.Informational, $"Sailfish: loaded settings from {settingsFile.FullName}");

        // If the file exists but cannot be parsed (malformed JSON, IO error, etc.) we intentionally let the
        // exception propagate. TestExecutor.HandleStartupException surfaces it to the test framework so the user
        // can see and fix their config — silently falling back to defaults previously hid these problems.
        return SailfishSettingsParser.Parse(settingsFile.FullName);
    }

    /// <summary>
    ///     Locates <c>.sailfish.json</c>, searching upward from the working directory first (historical
    ///     behaviour) and then, as a fallback, upward from the test assembly's own directory (the output dir).
    ///     Returns null when neither search finds it.
    /// </summary>
    private static FileInfo? LocateSettingsFile(string? testAssemblyLocation)
    {
#pragma warning disable RS1035
        var fromWorkingDirectory = TryRecurseUpwards(Directory.GetCurrentDirectory());
#pragma warning restore RS1035
        if (fromWorkingDirectory is not null) return fromWorkingDirectory;

        return string.IsNullOrEmpty(testAssemblyLocation)
            ? null
            : TryRecurseUpwards(testAssemblyLocation!);
    }

    private static FileInfo? TryRecurseUpwards(string startPath)
    {
        try
        {
            return DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".sailfish.json", startPath, 6);
        }
        catch (TestAdapterException)
        {
            return null;
        }
    }

    private static void Log(IMessageLogger? messageLogger, TestMessageLevel level, string message)
    {
        messageLogger?.SendMessage(level, message);
    }
}
