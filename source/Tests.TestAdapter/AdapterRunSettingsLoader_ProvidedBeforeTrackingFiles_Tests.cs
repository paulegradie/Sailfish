using System;
using System.IO;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

// Serialize with the sibling AdapterRunSettingsLoader tests — all mutate the process-wide current working
// directory via Directory.SetCurrentDirectory, which races under xUnit's default class-parallel execution.
[Collection("CwdMutatingAdapterRunSettingsLoader")]
public class AdapterRunSettingsLoaderProvidedBeforeTrackingFilesTests
{
    [Fact]
    public void ProvidedBeforeTrackingFiles_FromSailDiffSettings_FlowThroughToRunSettings()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_before_" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(nested);

        try
        {
            var json = """
            {
              "GlobalSettings": {},
              "SailDiffSettings": {
                "ProvidedBeforeTrackingFiles": [ "baseline/run-1.json.tracking", "baseline/run-2.json.tracking" ]
              }
            }
            """;
            File.WriteAllText(Path.Combine(root, ".sailfish.json"), json);

            Directory.SetCurrentDirectory(nested);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            runSettings.ProvidedBeforeTrackingFiles.ShouldBe(new[] { "baseline/run-1.json.tracking", "baseline/run-2.json.tracking" });
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void NoProvidedBeforeTrackingFiles_DefaultsToEmpty()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_before_empty_" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(nested);

        try
        {
            var json = """
            {
              "GlobalSettings": {},
              "SailDiffSettings": {}
            }
            """;
            File.WriteAllText(Path.Combine(root, ".sailfish.json"), json);

            Directory.SetCurrentDirectory(nested);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            runSettings.ProvidedBeforeTrackingFiles.ShouldBeEmpty();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }
}
