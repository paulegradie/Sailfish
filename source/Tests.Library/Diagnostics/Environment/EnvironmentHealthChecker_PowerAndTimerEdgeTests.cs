using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Diagnostics.Environment;
using Shouldly;
using Xunit;

namespace Tests.Library.Diagnostics.Environment;

[CollectionDefinition("EnvSerial", DisableParallelization = true)]
public class EnvSerialCollection { }

[Collection("EnvSerial")]
public class EnvironmentHealthChecker_PowerAndTimerEdgeTests
{
    private static HealthCheckEntry InvokePrivateCheckOsPowerHints()
    {
        var method = typeof(EnvironmentHealthChecker).GetMethod("CheckOsPowerHints", BindingFlags.NonPublic | BindingFlags.Static);
        method.ShouldNotBeNull();
        return (HealthCheckEntry)method!.Invoke(null, null)!;
    }

    [Fact]
    public void TimerResolution_UsesSleepNaN_WhenThreadInterrupted()
    {
        // Interrupt the test thread before it enters Sleep to trigger ThreadInterruptedException inside measurement
        HealthCheckEntry? result = null;
        var method = typeof(EnvironmentHealthChecker).GetMethod("CheckTimerResolution", BindingFlags.NonPublic | BindingFlags.Static);
        method.ShouldNotBeNull();

        var t = new Thread(() =>
        {
            result = (HealthCheckEntry)method!.Invoke(null, null)!;
        });

        t.Start();
        // Interrupt before it goes into Sleep
        t.Interrupt();
        t.Join();

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Timer");
        result.Details.ShouldContain("Sleep(1) median: n/a");
    }

    private static string CreateFakePowerCfg(string output)
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "sf_powercfg_" + Guid.NewGuid().ToString("N"))).FullName;
        var scriptPath = Path.Combine(dir, "powercfg.cmd");
        File.WriteAllText(scriptPath, "@echo off\r\n" + output + "\r\n");
        return dir;
    }

    [Fact]
    public void PowerPlan_Balanced_Warn_WhenBalanced()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var oldCwd = Directory.GetCurrentDirectory();
        var dir = CreateFakePowerCfg("echo Power Scheme GUID: 00000000-0000-0000-0000-000000000000 (Balanced)");
        try
        {
            Directory.SetCurrentDirectory(dir);
            var entry = InvokePrivateCheckOsPowerHints();
            entry.Name.ShouldBe("Power Plan");
            entry.Status.ShouldBe(HealthStatus.Warn);
            entry.Details.ShouldContain("Balanced");
        }
        finally
        {
            Directory.SetCurrentDirectory(oldCwd);
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void PowerPlan_Pass_WhenUltimate()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var oldCwd = Directory.GetCurrentDirectory();
        var dir = CreateFakePowerCfg("echo Power Scheme GUID: 00000000-0000-0000-0000-000000000000 (Ultimate Performance)");
        try
        {
            Directory.SetCurrentDirectory(dir);
            var entry = InvokePrivateCheckOsPowerHints();
            entry.Name.ShouldBe("Power Plan");
            entry.Status.ShouldBe(HealthStatus.Pass);
        }
        finally
        {
            Directory.SetCurrentDirectory(oldCwd);
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void PowerPlan_Unknown_WhenNoOutput()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var oldCwd = Directory.GetCurrentDirectory();
        var dir = CreateFakePowerCfg(""); // no output
        try
        {
            Directory.SetCurrentDirectory(dir);
            var entry = InvokePrivateCheckOsPowerHints();
            entry.Name.ShouldBe("Power Plan");
            entry.Status.ShouldBe(HealthStatus.Unknown);
        }
        finally
        {
            Directory.SetCurrentDirectory(oldCwd);
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void PowerPlan_ParseFallback_NoParens()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var oldCwd = Directory.GetCurrentDirectory();
        var dir = CreateFakePowerCfg("echo Balanced");
        try
        {
            Directory.SetCurrentDirectory(dir);
            var entry = InvokePrivateCheckOsPowerHints();
            entry.Name.ShouldBe("Power Plan");
            entry.Status.ShouldBe(HealthStatus.Warn);
        }
        finally
        {
            Directory.SetCurrentDirectory(oldCwd);
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    private sealed class ThrowingTimerProvider : Sailfish.Execution.ITimerCalibrationResultProvider
    {
        public Sailfish.Execution.TimerCalibrationResult? Current => throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task TimerJitter_ReturnsUnknown_WhenProviderThrows()
    {
        var checker = new EnvironmentHealthChecker(new ThrowingTimerProvider());
        var report = await checker.CheckAsync();
        var entry = report.Entries.First(e => e.Name == "Timer Jitter");
        entry.Status.ShouldBe(HealthStatus.Unknown);
        entry.Details.ShouldContain("boom");
    }
}

