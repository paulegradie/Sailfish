using System.IO;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestSettingsParser;
using SailDiffSettings = Sailfish.Analysis.SailDiff.SailDiffSettings;
using RuntimeScaleFishSettings = Sailfish.Analysis.ScaleFish.ScaleFishSettings;
using RuntimeTrawlSettings = Sailfish.Trawl.TrawlSettings;
using CoreAiAnalysisSettings = Sailfish.Analysis.Ai.AiAnalysisSettings;

namespace Sailfish.TestAdapter.Execution;

public static class AdapterRunSettingsLoader
{
    public static IRunSettings RetrieveAndLoadAdapterRunSettings()
    {
        var parsedSettings = ParseSettings();

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

        // Skipper AI analysis is opt-in via .sailfish.json. A custom ISailfishAgent must also be registered
        // through IRegisterSailfishServices; without one, enabling this is a harmless no-op.
        if (parsedSettings.AiAnalysisSettings is { Enabled: true } ai)
            runSettingsBuilder = runSettingsBuilder.WithAiAnalysis(new CoreAiAnalysisSettings(
                writeReviewArtifact: ai.WriteReviewArtifact ?? true,
                emitConsoleSummary: ai.EmitConsoleSummary ?? true,
                useResponseCache: ai.UseResponseCache ?? true));

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

        return mappedSettings;
    }

    private static SettingsConfiguration ParseSettings()
    {
        FileInfo settingsFile;
        try
        {
            settingsFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(
                ".sailfish.json",
#pragma warning disable RS1035
                Directory.GetCurrentDirectory(),
#pragma warning restore RS1035
                6);
        }
        catch (TestAdapterException)
        {
            // No .sailfish.json is present anywhere up the tree — that is an expected,
            // supported configuration. Fall back to defaults silently.
            return new SettingsConfiguration();
        }

        // If the file exists but cannot be parsed (malformed JSON, IO error, etc.) we
        // intentionally let the exception propagate. TestExecutor.HandleStartupException
        // surfaces it to the test framework so the user can see and fix their config —
        // silently falling back to defaults previously hid these problems entirely.
        return SailfishSettingsParser.Parse(settingsFile.FullName);
    }
}