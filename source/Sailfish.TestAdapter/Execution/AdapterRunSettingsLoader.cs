using System.IO;
using Autofac;
using Sailfish.Analysis.Saildiff;
using Sailfish.Exceptions;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestSettingsParser;

namespace Sailfish.TestAdapter.Execution;

public static class AdapterRunSettingsLoaderExtensionMethods
{
    public static void RegisterRunSettings(this ContainerBuilder builder)
    {
        var sailfishSettings = RetrieveRunSettings();
        builder.RegisterInstance(sailfishSettings);
    }

    private static IRunSettings RetrieveRunSettings()
    {
        var parsedSettings = ParseSettings();

        if (parsedSettings.TestSettings.DisableEverything) throw new SailfishException("Everything is disabled!");
        
        var runSettingsBuilder = RunSettingsBuilder.CreateBuilder();
        if (!string.IsNullOrEmpty(parsedSettings.TestSettings.ResultsDirectory))
        {
            runSettingsBuilder.WithLocalOutputDirectory(parsedSettings.TestSettings.ResultsDirectory);
        }

        if (parsedSettings.TestSettings.Disabled)
        {
            runSettingsBuilder.WithAnalysisDisabledGlobally();
        }

        if (parsedSettings.TestSettings.DisableOverheadEstimation)
        {
            runSettingsBuilder.DisableOverheadEstimation();
        }

        var testSettings = MapToTestSettings(parsedSettings.TestSettings);
        var runSettings = runSettingsBuilder
            .CreateTrackingFiles()
            .WithAnalysis()
            .WithComplexityAnalysis()
            .WithAnalysisTestSettings(testSettings)
            .Build();
        return runSettings;
    }

    private static TestSettings MapToTestSettings(SailfishTestSettings settings)
    {
        if (settings?.Resolution is not null)
        {
            // TODO: Modify this when we impl resolution settings throughout (or ditch the idea)
            // settingsBuilder.WithResolution(settings.Resolution);
        }

        var mappedSettings = new TestSettings();
        if (settings?.TestType is not null)
        {
            mappedSettings.SetTestType(settings.TestType);
        }

        if (settings?.UseInnerQuartile is not null)
        {
            mappedSettings.SetUseInnerQuartile(settings.UseInnerQuartile);
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