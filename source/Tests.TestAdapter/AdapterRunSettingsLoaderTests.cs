using System;
using System.IO;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

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
}

