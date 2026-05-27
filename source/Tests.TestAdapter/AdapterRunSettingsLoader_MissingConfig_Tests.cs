using System;
using System.IO;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

// Serialize with the other AdapterRunSettingsLoader tests — all of them mutate the process-wide
// current working directory via Directory.SetCurrentDirectory, which races catastrophically
// under xUnit's default class-parallel execution.
[Collection("CwdMutatingAdapterRunSettingsLoader")]
public class AdapterRunSettingsLoaderMissingConfigTests
{
    // Covers the catch (TestAdapterException) arm in AdapterRunSettingsLoader.ParseSettings:
    // when no .sailfish.json exists anywhere up the directory tree (within the 6-level
    // recursion limit), the loader should fall back to defaults silently rather than
    // surfacing the "couldn't locate" error to the user.
    [Fact]
    public void Missing_SailfishJson_Returns_Defaults_Without_Throwing()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        // Place the working dir directly under the system temp root so the upward
        // recursion (max 6 parents) walks only through filesystem roots that won't
        // contain a .sailfish.json.
        var root = Path.Combine(Path.GetTempPath(), "sf_adapter_settings_missing_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            Directory.SetCurrentDirectory(root);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            // Default SettingsConfiguration → EnableEnvironmentHealthCheck defaults to true.
            // (The defaults-from-config test verifies the same against an empty config file;
            // this test verifies the same against no config file at all.)
            runSettings.EnableEnvironmentHealthCheck.ShouldBeTrue();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }
}
