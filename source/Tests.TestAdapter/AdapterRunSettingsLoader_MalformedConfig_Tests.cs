using System;
using System.IO;
using System.Text.Json;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

// Serialize with the other AdapterRunSettingsLoader tests — all of them mutate the process-wide
// current working directory via Directory.SetCurrentDirectory, which races catastrophically
// under xUnit's default class-parallel execution.
[Collection("CwdMutatingAdapterRunSettingsLoader")]
public class AdapterRunSettingsLoaderMalformedConfigTests
{
    // Regression test for the previously-silent failure mode where any exception thrown while
    // loading .sailfish.json was swallowed and defaults were returned, leaving the user with
    // no signal that their config was being ignored. We now propagate parse failures so
    // TestExecutor.HandleStartupException surfaces them to the test framework.
    [Fact]
    public void Malformed_SailfishJson_Surfaces_Parse_Error_Instead_Of_Silently_Falling_Back()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_settings_malformed_" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(nested);

        try
        {
            // Intentionally broken JSON — unterminated object.
            File.WriteAllText(Path.Combine(root, ".sailfish.json"), "{ \"GlobalSettings\": ");

            Directory.SetCurrentDirectory(nested);

            Should.Throw<JsonException>(() => AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings());
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }
}
