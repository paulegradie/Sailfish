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

        if (parsedSettings.DisableEverything) throw new SailfishException("Everything is disabled!");

        var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
        if (!string.IsNullOrEmpty(parsedSettings.ResultsDirectory))
        {
            runSettingsBuilder = runSettingsBuilder.WithLocalOutputDirectory(parsedSettings.ResultsDirectory);
        }

        if (parsedSettings.SailDiffSettings.Disabled)
        {
            runSettingsBuilder = runSettingsBuilder.WithAnalysisDisabledGlobally();
        }

        if (parsedSettings.DisableOverheadEstimation)
        {
            runSettingsBuilder = runSettingsBuilder.DisableOverheadEstimation();
        }

        var testSettings = MapToTestSettings(parsedSettings);
        var runSettings = runSettingsBuilder
            .CreateTrackingFiles()
            .WithSailDiff(testSettings)
            .WithScalefish()
            .Build();
        return runSettings;
    }

    private static Analysis.SailDiff.SailDiffSettings MapToTestSettings(SailfishSettings settings)
    {
        var mappedSettings = new Analysis.SailDiff.SailDiffSettings();
        if (settings?.SailDiffSettings.TestType is not null)
        {
            mappedSettings.SetTestType(settings.SailDiffSettings.TestType);
        }

        if (settings?.UseOutlierDetection is not null)
        {
            mappedSettings.SetUseOutlierDetection(settings.UseOutlierDetection);
        }

        if (settings?.SailDiffSettings.Alpha is not null)
        {
            mappedSettings.SetAlpha(settings.SailDiffSettings.Alpha);
        }

        if (settings?.Round is not null)
        {
            mappedSettings.SetRound(settings.Round);
        }

        return mappedSettings;
    }

    private static SailfishSettings ParseSettings()
    {
        var parsedSettings = new SailfishSettings();
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