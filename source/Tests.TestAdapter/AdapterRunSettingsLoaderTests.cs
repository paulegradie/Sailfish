using System;
using System.IO;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

// Serialize with sibling AdapterRunSettingsLoaderDefaultsTests — both mutate the
// process-wide current working directory via Directory.SetCurrentDirectory, which races
// catastrophically under xUnit's default class-parallel execution.
[Collection("CwdMutatingAdapterRunSettingsLoader")]
public class AdapterRunSettingsLoaderTests
{
    [Fact]
    public void EnableEnvironmentHealthCheck_Is_Parsed_From_Settings()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_settings_" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(nested);

        try
        {
            var json = """
            {
              "SailfishSettings": {
                "EnableEnvironmentHealthCheck": false
              },
              "GlobalSettings": {},
              "SailDiffSettings": {},
              "ScaleFishSettings": {}
            }
            """;
            File.WriteAllText(Path.Combine(root, ".sailfish.json"), json);

            Directory.SetCurrentDirectory(nested);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();
            runSettings.EnableEnvironmentHealthCheck.ShouldBeFalse();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }

    [Theory]
    [InlineData("\"GlobalSettings\": { \"DistributionPlotStyle\": \"BoxPlot\" }, \"SailDiffSettings\": {}")]
    [InlineData("\"GlobalSettings\": {}, \"SailDiffSettings\": { \"DistributionPlotStyle\": \"BoxPlot\" }")] // convenience fallback
    public void DistributionPlotStyle_BoxPlot_IsParsed_FromEitherSection(string sections)
    {
        var json = "{ " + sections + ", \"SailfishSettings\": {}, \"ScaleFishSettings\": {} }";
        LoadFromConfig(json).DistributionPlotStyle.ShouldBe(DistributionPlotStyle.BoxPlot);
    }

    [Fact]
    public void DistributionPlotStyle_DefaultsToHistogram_WhenUnset()
    {
        var json = "{ \"GlobalSettings\": {}, \"SailDiffSettings\": {}, \"SailfishSettings\": {}, \"ScaleFishSettings\": {} }";
        LoadFromConfig(json).DistributionPlotStyle.ShouldBe(DistributionPlotStyle.Histogram);
    }

    // Writes the config in a parent dir and runs from a nested dir, because the loader recurses
    // UPWARDS to find .sailfish.json (it doesn't match a file sitting in the current directory).
    private static Sailfish.Contracts.Public.Models.IRunSettings LoadFromConfig(string json)
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_settings_" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(nested);

        try
        {
            File.WriteAllText(Path.Combine(root, ".sailfish.json"), json);
            Directory.SetCurrentDirectory(nested);
            return AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }
}

