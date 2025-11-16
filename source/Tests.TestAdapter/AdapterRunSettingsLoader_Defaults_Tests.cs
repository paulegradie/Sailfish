using System;
using System.IO;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

public class AdapterRunSettingsLoaderDefaultsTests
{
    [Fact]
    public void EnableEnvironmentHealthCheck_Defaults_To_True_When_Not_Specified()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_settings_default_" + Guid.NewGuid().ToString("N"));
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
            runSettings.EnableEnvironmentHealthCheck.ShouldBeTrue();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }
}

