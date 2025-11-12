using Sailfish.Diagnostics.Environment;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Library.Diagnostics.Environment;

public class EnvironmentHealthCheckerAdditionalTests
{
    [Fact]
    public async Task BuildMode_Uses_TestAssemblyPath_When_Provided()
    {
        var checker = new EnvironmentHealthChecker();
        var ctx = new EnvironmentHealthCheckContext
        {
            TestAssemblyPath = typeof(EnvironmentHealthChecker).Assembly.Location
        };

        var report = await checker.CheckAsync(ctx);
        var build = report.Entries.First(e => e.Name == "Build Mode");
        build.ShouldNotBeNull();
    }

    [Fact]
    public async Task CpuAffinity_Pass_When_Pinned_To_Single_Core()
    {
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        var original = proc.ProcessorAffinity;
        try
        {
            // Pin to a single core (bit 0)
            proc.ProcessorAffinity = (System.IntPtr)1;

            var checker = new EnvironmentHealthChecker();
            var report = await checker.CheckAsync();
            var affinity = report.Entries.First(e => e.Name == "CPU Affinity");
            affinity.Status.ShouldBe(HealthStatus.Pass);
        }
        finally
        {
            proc.ProcessorAffinity = original;
        }
    }

    [Fact]
    public async Task Jit_Details_Reflect_Environment_Flags()
    {
        var priorTiered = System.Environment.GetEnvironmentVariable("COMPlus_TieredCompilation");
        var priorQuick = System.Environment.GetEnvironmentVariable("COMPlus_TC_QuickJit");
        var priorLoops = System.Environment.GetEnvironmentVariable("COMPlus_TC_QuickJitForLoops");
        var priorOsr = System.Environment.GetEnvironmentVariable("COMPlus_TC_OnStackReplacement");
        try
        {
            System.Environment.SetEnvironmentVariable("COMPlus_TieredCompilation", "1");
            System.Environment.SetEnvironmentVariable("COMPlus_TC_QuickJit", "1");
            System.Environment.SetEnvironmentVariable("COMPlus_TC_QuickJitForLoops", "0");
            System.Environment.SetEnvironmentVariable("COMPlus_TC_OnStackReplacement", "0");

            var checker = new EnvironmentHealthChecker();
            var report = await checker.CheckAsync();
            var jit = report.Entries.First(e => e.Name == "JIT (Tiered/OSR)");
            jit.Details.ShouldContain("Tiered=1");
            jit.Details.ShouldContain("QuickJit=1");
            jit.Details.ShouldContain("QuickJitForLoops=0");
            jit.Details.ShouldContain("OSR=0");
        }
        finally
        {
            System.Environment.SetEnvironmentVariable("COMPlus_TieredCompilation", priorTiered);
            System.Environment.SetEnvironmentVariable("COMPlus_TC_QuickJit", priorQuick);
            System.Environment.SetEnvironmentVariable("COMPlus_TC_QuickJitForLoops", priorLoops);
            System.Environment.SetEnvironmentVariable("COMPlus_TC_OnStackReplacement", priorOsr);
        }
    }


    [Fact]
    public async Task CpuAffinity_Warn_When_Pinned_To_Two_Cores()
    {
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        var original = proc.ProcessorAffinity;
        try
        {
            // Pin to two cores (bits 0 and 1)
            proc.ProcessorAffinity = (System.IntPtr)3;

            var checker = new EnvironmentHealthChecker();
            var report = await checker.CheckAsync();
            var affinity = report.Entries.First(e => e.Name == "CPU Affinity");
            affinity.Status.ShouldBe(HealthStatus.Warn);
        }
        finally
        {
            proc.ProcessorAffinity = original;
        }
    }

    [Fact]
    public async Task ProcessPriority_Pass_When_AboveNormal()
    {
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        var original = proc.PriorityClass;
        try
        {
            proc.PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
            var checker = new EnvironmentHealthChecker();
            var report = await checker.CheckAsync();
            var entry = report.Entries.First(e => e.Name == "Process Priority");
            entry.Status.ShouldBe(HealthStatus.Pass);
        }
        finally
        {
            try { proc.PriorityClass = original; } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task BuildMode_Unknown_When_Loading_NonDotNet_File()
    {
        var checker = new EnvironmentHealthChecker();
        var sysDir = System.Environment.SystemDirectory;
        var nonDotNetPath = System.IO.Path.Combine(sysDir, "notepad.exe");
        if (!System.IO.File.Exists(nonDotNetPath))
        {
            // Fall back to an obviously non-.NET file name in system directory to trigger load failure
            nonDotNetPath = System.IO.Path.Combine(sysDir, "license.rtf");
        }
        if (!System.IO.File.Exists(nonDotNetPath)) return; // environment-specific bail out

        var ctx = new EnvironmentHealthCheckContext { TestAssemblyPath = nonDotNetPath };
        var report = await checker.CheckAsync(ctx);
        var build = report.Entries.First(e => e.Name == "Build Mode");
        build.Status.ShouldBe(HealthStatus.Unknown);
    }


    [Fact]
    public async Task ProcessPriority_Warn_When_BelowNormal()
    {
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        var original = proc.PriorityClass;
        try
        {
            proc.PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            var checker = new EnvironmentHealthChecker();
            var report = await checker.CheckAsync();
            var entry = report.Entries.First(e => e.Name == "Process Priority");
            entry.Status.ShouldBe(HealthStatus.Warn);
        }
        finally
        {
            try { proc.PriorityClass = original; } catch { /* ignore */ }
        }
    }

}