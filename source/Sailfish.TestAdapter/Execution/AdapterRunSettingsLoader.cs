using System.IO;
using Sailfish.Exceptions;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestSettingsParser;


namespace Sailfish.TestAdapter.Execution;

public static class AdapterRunSettingsLoader
{
    public static IRunSettings LoadAdapterRunSettings()
    {
        return RetrieveRunSettings();
    }

    private static IRunSettings RetrieveRunSettings()
    {
        var parsedSettings = ParseSettings();

        if (parsedSettings.GlobalSettings.DisableEverything) throw new SailfishException("Everything is disabled!");

        var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
        if (!string.IsNullOrEmpty(parsedSettings.GlobalSettings.ResultsDirectory))
        {
            runSettingsBuilder = runSettingsBuilder.WithLocalOutputDirectory(parsedSettings.GlobalSettings.ResultsDirectory);
        }

        if (parsedSettings.SailDiffSettings.Disabled)
        {
            runSettingsBuilder = runSettingsBuilder.WithAnalysisDisabledGlobally();
        }

        if (parsedSettings.SailfishSettings.DisableOverheadEstimation)
        {
            runSettingsBuilder = runSettingsBuilder.DisableOverheadEstimation();
        }

        if (parsedSettings.SailfishSettings.SampleSizeOverride is not null)
        {
            runSettingsBuilder = runSettingsBuilder.WithGlobalSampleSize(parsedSettings.SailfishSettings.SampleSizeOverride.Value);
        }

        if (parsedSettings.SailfishSettings.NumWarmupIterationsOverride is not null)
        {
            runSettingsBuilder = runSettingsBuilder.WithGlobalNumWarmupIterations(parsedSettings.SailfishSettings.NumWarmupIterationsOverride.Value);
        }

        var testSettings = MapToTestSettings(parsedSettings);
        var runSettings = runSettingsBuilder
            .CreateTrackingFiles()
            .WithSailDiff(testSettings)
            .WithScalefish()
            .Build();
        return runSettings;
    }

    private static Analysis.SailDiff.SailDiffSettings MapToTestSettings(SettingsConfiguration settingsConfiguration)
    {
        var mappedSettings = new Analysis.SailDiff.SailDiffSettings();
        if (settingsConfiguration?.SailDiffSettings.TestType is not null)
        {
            mappedSettings.SetTestType(settingsConfiguration.SailDiffSettings.TestType);
        }

        if (settingsConfiguration?.GlobalSettings.UseOutlierDetection is not null)
        {
            mappedSettings.SetUseOutlierDetection(settingsConfiguration.GlobalSettings.UseOutlierDetection);
        }

        if (settingsConfiguration?.SailDiffSettings.Alpha is not null)
        {
            mappedSettings.SetAlpha(settingsConfiguration.SailDiffSettings.Alpha);
        }

        if (settingsConfiguration?.GlobalSettings.Round is not null)
        {
            mappedSettings.SetRound(settingsConfiguration.GlobalSettings.Round);
        }

        return mappedSettings;
    }

    private static SettingsConfiguration ParseSettings()
    {
        var parsedSettings = new SettingsConfiguration();
        try
        {
            var settingsFile = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(
                ".sailfish.json",
                Directory.GetCurrentDirectory(),
                6);
            return SailfishSettingsParser.Parse(settingsFile.FullName);
        }
        catch
        {
            return parsedSettings;
        }
    }
}