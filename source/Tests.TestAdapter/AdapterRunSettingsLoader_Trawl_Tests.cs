using System;
using System.IO;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

// Serialize with the sibling AdapterRunSettingsLoader tests — they all mutate the process-wide
// current working directory via Directory.SetCurrentDirectory, which races under xUnit's default
// class-parallel execution.
[Collection("CwdMutatingAdapterRunSettingsLoader")]
public class AdapterRunSettingsLoaderTrawlTests
{
    [Fact]
    public void TrawlSettings_Are_Mapped_From_Json()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_trawl_" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(nested);

        try
        {
            var json = """
            {
              "GlobalSettings": {},
              "SailDiffSettings": {},
              "ScaleFishSettings": {},
              "TrawlSettings": {
                "Disabled": true,
                "VirtualUsersOverride": 4,
                "MaxDurationSecondsOverride": 12.5,
                "WarmupSecondsOverride": 2.0
              }
            }
            """;
            File.WriteAllText(Path.Combine(root, ".sailfish.json"), json);

            Directory.SetCurrentDirectory(nested);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            runSettings.TrawlSettings.Disabled.ShouldBeTrue();
            runSettings.TrawlSettings.VirtualUsersOverride.ShouldBe(4);
            runSettings.TrawlSettings.MaxDurationSecondsOverride.ShouldBe(12.5);
            runSettings.TrawlSettings.WarmupSecondsOverride.ShouldBe(2.0);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void TrawlSettings_Default_To_NoOverride_When_Absent()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_trawl_default_" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(nested);

        try
        {
            var json = """
            {
              "GlobalSettings": {},
              "SailDiffSettings": {},
              "ScaleFishSettings": {}
            }
            """;
            File.WriteAllText(Path.Combine(root, ".sailfish.json"), json);

            Directory.SetCurrentDirectory(nested);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            runSettings.TrawlSettings.ShouldNotBeNull();
            runSettings.TrawlSettings.Disabled.ShouldBeFalse();
            runSettings.TrawlSettings.VirtualUsersOverride.ShouldBeNull();
            runSettings.TrawlSettings.MaxDurationSecondsOverride.ShouldBeNull();
            runSettings.TrawlSettings.WarmupSecondsOverride.ShouldBeNull();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }
}
