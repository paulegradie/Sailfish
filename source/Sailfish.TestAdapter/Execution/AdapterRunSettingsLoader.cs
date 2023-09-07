using System.IO;
using Sailfish.Analysis.SailDiff;
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

        if (parsedSettings.TestSettings.DisableEverything) throw new SailfishException("Everything is disabled!");

        var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
        if (!string.IsNullOrEmpty(parsedSettings.TestSettings.ResultsDirectory))
        {
            runSettingsBuilder = runSettingsBuilder.WithLocalOutputDirectory(parsedSettings.TestSettings.ResultsDirectory);
        }

        if (parsedSettings.TestSettings.Disabled)
        {
            runSettingsBuilder = runSettingsBuilder.WithAnalysisDisabledGlobally();
        }

        if (parsedSettings.TestSettings.DisableOverheadEstimation)
        {
            runSettingsBuilder = runSettingsBuilder.DisableOverheadEstimation();
        }

        var testSettings = MapToTestSettings(parsedSettings.TestSettings);
        var runSettings = runSettingsBuilder
            .CreateTrackingFiles()
            .WithSailDiff(testSettings)
            .WithScalefish()
            .Build();
        return runSettings;
    }

    private static SailDiffSettings MapToTestSettings(SailfishTestSettings settings)
    {
        if (settings?.Resolution is not null)
        {
            // TODO: Modify this when we impl resolution settings throughout (or ditch the idea)
            // settingsBuilder.WithResolution(settings.Resolution);
        }

        var mappedSettings = new SailDiffSettings();
        if (settings?.TestType is not null)
        {
            mappedSettings.SetTestType(settings.TestType);
        }

        if (settings?.UseOutlierDetection is not null)
        {
            mappedSettings.SetUseOutlierDetection(settings.UseOutlierDetection);
        }

        if (settings?.Alpha is not null)
        {
            mappedSettings.SetAlpha(settings.Alpha);
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