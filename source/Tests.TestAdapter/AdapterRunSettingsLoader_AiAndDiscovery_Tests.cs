using System;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Execution;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

// Serialize with the sibling AdapterRunSettingsLoader tests — all mutate the process-wide current working
// directory via Directory.SetCurrentDirectory, which races under xUnit's default class-parallel execution.
[Collection("CwdMutatingAdapterRunSettingsLoader")]
public class AdapterRunSettingsLoaderAiAndDiscoveryTests
{
    private const string TrackingFileName = "PerformanceTracking_20200101_000000.json.tracking";

    // ---- #3 Robust discovery + observability -------------------------------------------------------------

    [Fact]
    public void Discovery_FindsSettingsBesideTheTestAssembly_WhenWorkingDirectoryHasNone()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var cwdRoot = NewTempDir("sf_disc_cwd");          // isolated, no .sailfish.json up-tree
        var asmDir = NewTempDir("sf_disc_asm");           // a .sailfish.json sits here, beside the "assembly"
        var nestedCwd = Path.Combine(cwdRoot, "a", "b");
        Directory.CreateDirectory(nestedCwd);

        // Distinctive setting so we can prove THIS file was loaded (default would be true).
        File.WriteAllText(Path.Combine(asmDir, ".sailfish.json"),
            """{ "SailfishSettings": { "EnableEnvironmentHealthCheck": false } }""");

        var logger = Substitute.For<IMessageLogger>();
        try
        {
            Directory.SetCurrentDirectory(nestedCwd);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings(
                Path.Combine(asmDir, "SomeTests.dll"), logger);

            runSettings.EnableEnvironmentHealthCheck.ShouldBeFalse(); // proves the assembly-dir file was loaded
            logger.Received().SendMessage(
                TestMessageLevel.Informational,
                Arg.Is<string>(s => s.Contains("loaded settings from")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            TryDelete(cwdRoot);
            TryDelete(asmDir);
        }
    }

    [Fact]
    public void Discovery_WarnsAndFallsBackToDefaults_WhenNoSettingsAnywhere()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var cwdRoot = NewTempDir("sf_none_cwd");
        var asmDir = NewTempDir("sf_none_asm");
        var nestedCwd = Path.Combine(cwdRoot, "a", "b");
        Directory.CreateDirectory(nestedCwd);

        var logger = Substitute.For<IMessageLogger>();
        try
        {
            Directory.SetCurrentDirectory(nestedCwd);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings(
                Path.Combine(asmDir, "SomeTests.dll"), logger);

            // A warning (not debug) is the whole point — AI being off must never be silent.
            logger.Received().SendMessage(
                TestMessageLevel.Warning,
                Arg.Is<string>(s => s.Contains("no .sailfish.json") && s.Contains("AiAnalysisSettings.Enabled")));
            runSettings.EnableEnvironmentHealthCheck.ShouldBeTrue(); // defaults are in effect
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            TryDelete(cwdRoot);
            TryDelete(asmDir);
        }
    }

    // ---- #4 Opt-in auto-baseline for run-vs-run SailDiff --------------------------------------------------

    [Fact]
    public void AutoCompareToPreviousRun_ResolvesMostRecentPriorTrackingFile()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var cwdRoot = NewTempDir("sf_auto_cwd");
        var outputDir = NewTempDir("sf_auto_out");
        var nestedCwd = Path.Combine(cwdRoot, "a", "b");
        Directory.CreateDirectory(nestedCwd);

        var trackingDir = Path.Combine(outputDir, "sailfish_tracking_output");
        Directory.CreateDirectory(trackingDir);
        var priorRun = Path.Combine(trackingDir, TrackingFileName);
        File.WriteAllText(priorRun, "{}");

        // .sailfish.json found via cwd-search, points its output dir at our absolute temp dir, opts in.
        File.WriteAllText(Path.Combine(cwdRoot, ".sailfish.json"),
            $$"""
            {
              "GlobalSettings": { "ResultsDirectory": {{ Json(outputDir) }} },
              "SailDiffSettings": { "AutoCompareToPreviousRun": true }
            }
            """);

        try
        {
            Directory.SetCurrentDirectory(nestedCwd);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            runSettings.ProvidedBeforeTrackingFiles.ShouldHaveSingleItem().ShouldEndWith(TrackingFileName);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            TryDelete(cwdRoot);
            TryDelete(outputDir);
        }
    }

    [Fact]
    public void AutoCompareToPreviousRun_OnFirstRunWithNoPriorFile_LeavesComparisonUnset()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var cwdRoot = NewTempDir("sf_auto_first_cwd");
        var outputDir = NewTempDir("sf_auto_first_out"); // exists but contains no tracking files
        var nestedCwd = Path.Combine(cwdRoot, "a", "b");
        Directory.CreateDirectory(nestedCwd);

        File.WriteAllText(Path.Combine(cwdRoot, ".sailfish.json"),
            $$"""
            {
              "GlobalSettings": { "ResultsDirectory": {{ Json(outputDir) }} },
              "SailDiffSettings": { "AutoCompareToPreviousRun": true }
            }
            """);

        try
        {
            Directory.SetCurrentDirectory(nestedCwd);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            runSettings.ProvidedBeforeTrackingFiles.ShouldBeEmpty();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            TryDelete(cwdRoot);
            TryDelete(outputDir);
        }
    }

    [Fact]
    public void ExplicitProvidedBeforeTrackingFiles_TakePrecedenceOverAutoCompare()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var cwdRoot = NewTempDir("sf_auto_explicit_cwd");
        var outputDir = NewTempDir("sf_auto_explicit_out");
        var nestedCwd = Path.Combine(cwdRoot, "a", "b");
        Directory.CreateDirectory(nestedCwd);

        // A prior file exists AND auto-compare is on, but an explicit file is named — the explicit one wins.
        var trackingDir = Path.Combine(outputDir, "sailfish_tracking_output");
        Directory.CreateDirectory(trackingDir);
        File.WriteAllText(Path.Combine(trackingDir, TrackingFileName), "{}");

        File.WriteAllText(Path.Combine(cwdRoot, ".sailfish.json"),
            $$"""
            {
              "GlobalSettings": { "ResultsDirectory": {{ Json(outputDir) }} },
              "SailDiffSettings": {
                "AutoCompareToPreviousRun": true,
                "ProvidedBeforeTrackingFiles": [ "explicit/run-1.json.tracking" ]
              }
            }
            """);

        try
        {
            Directory.SetCurrentDirectory(nestedCwd);

            var runSettings = AdapterRunSettingsLoader.RetrieveAndLoadAdapterRunSettings();

            runSettings.ProvidedBeforeTrackingFiles.ShouldBe(new[] { "explicit/run-1.json.tracking" });
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            TryDelete(cwdRoot);
            TryDelete(outputDir);
        }
    }

    private static string Json(string value) => System.Text.Json.JsonSerializer.Serialize(value);

    private static string NewTempDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), prefix + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch
        {
            /* best-effort cleanup */
        }
    }
}
