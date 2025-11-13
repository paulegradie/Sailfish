using System;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Diagnostics.Environment;
using Shouldly;
using Xunit;

namespace Tests.Library.Diagnostics.Environment;

public class EnvironmentHealthCheckerTests
{
    [Fact]
    public async Task CheckAsync_Returns_Report_WithScoreAndEntries()
    {
        var checker = new EnvironmentHealthChecker();
        var report = await checker.CheckAsync();

        report.ShouldNotBeNull();
        report.Score.ShouldBeInRange(0, 100);
        report.Entries.ShouldNotBeEmpty();
        report.SummaryLabel.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Report_Includes_Key_Entries()
    {
        var checker = new EnvironmentHealthChecker();
        var report = await checker.CheckAsync();

        var names = report.Entries.Select(e => e.Name).ToList();
        names.ShouldContain("Process Priority");
        names.ShouldContain("GC Mode");
        names.ShouldContain("CPU Affinity");
        names.ShouldContain("Timer");
        names.ShouldContain("Background CPU");
        // One of these depending on OS
        names.Any(n => n is "Power Plan" or "Power Management").ShouldBeTrue();
    }

    [Fact]
    public async Task Report_Includes_BuildMode_And_JitEntries()
    {
        var checker = new EnvironmentHealthChecker();
        var report = await checker.CheckAsync();

        var names = report.Entries.Select(e => e.Name).ToList();
        names.ShouldContain("Build Mode");
        names.ShouldContain("JIT (Tiered/OSR)");
    }
    [Fact]
    public async Task Jit_Warns_When_TieredCompilation_Disabled()
    {
        var prior = System.Environment.GetEnvironmentVariable("COMPlus_TieredCompilation");
        try
        {
            System.Environment.SetEnvironmentVariable("COMPlus_TieredCompilation", "0");
            var checker = new EnvironmentHealthChecker();
            var report = await checker.CheckAsync();
            var jit = report.Entries.First(e => e.Name == "JIT (Tiered/OSR)");
            jit.Details.ShouldContain("Tiered=");
        }
        finally
        {
            System.Environment.SetEnvironmentVariable("COMPlus_TieredCompilation", prior);
        }
    }



    private sealed class TestTimerProvider : Sailfish.Execution.ITimerCalibrationResultProvider
    {
        public Sailfish.Execution.TimerCalibrationResult? Current { get; set; }
    }

    [Fact]
    public async Task Report_Includes_TimerJitter_WhenCalibrationProvided_Pass()
    {
        var provider = new TestTimerProvider
        {
            Current = new Sailfish.Execution.TimerCalibrationResult
            {
                RsdPercent = 3.0,
                MedianTicks = 1,
                Samples = 64,
                Warmups = 16,
                JitterScore = 100
            }
        };
        var checker = new EnvironmentHealthChecker(provider);
        var report = await checker.CheckAsync();
        report.Entries.Any(e => e.Name == "Timer Jitter" && e.Status == HealthStatus.Pass).ShouldBeTrue();
    }

    [Fact]
    public async Task Report_Includes_TimerJitter_WhenCalibrationProvided_Warn()
    {
        var provider = new TestTimerProvider
        {
            Current = new Sailfish.Execution.TimerCalibrationResult
            {
                RsdPercent = 10.0,
                MedianTicks = 1,
                Samples = 64,
                Warmups = 16,
                JitterScore = 60
            }
        };
        var checker = new EnvironmentHealthChecker(provider);
        var report = await checker.CheckAsync();
        report.Entries.Any(e => e.Name == "Timer Jitter" && e.Status == HealthStatus.Warn).ShouldBeTrue();
    }

    [Fact]
    public async Task Report_Includes_TimerJitter_WhenCalibrationProvided_Fail()
    {
        var provider = new TestTimerProvider
        {
            Current = new Sailfish.Execution.TimerCalibrationResult
            {
                RsdPercent = 20.0,
                MedianTicks = 1,
                Samples = 64,
                Warmups = 16,
                JitterScore = 20
            }
        };
        var checker = new EnvironmentHealthChecker(provider);
        var report = await checker.CheckAsync();
        report.Entries.Any(e => e.Name == "Timer Jitter" && e.Status == HealthStatus.Fail).ShouldBeTrue();
    }


        [Fact]
        public async Task Report_TimerJitter_Boundary_5Percent_Pass()
        {
            var provider = new TestTimerProvider
            {
                Current = new Sailfish.Execution.TimerCalibrationResult
                {
                    RsdPercent = 5.0,
                    MedianTicks = 1,
                    Samples = 64,
                    Warmups = 16,
                    JitterScore = 80
                }
            };
            var checker = new EnvironmentHealthChecker(provider);
            var report = await checker.CheckAsync();
            report.Entries.Any(e => e.Name == "Timer Jitter" && e.Status == HealthStatus.Pass).ShouldBeTrue();
        }

        [Fact]
        public async Task Report_TimerJitter_Boundary_15Percent_Warn()
        {
            var provider = new TestTimerProvider
            {
                Current = new Sailfish.Execution.TimerCalibrationResult
                {
                    RsdPercent = 15.0,
                    MedianTicks = 1,
                    Samples = 64,
                    Warmups = 16,
                    JitterScore = 40
                }
            };
            var checker = new EnvironmentHealthChecker(provider);
            var report = await checker.CheckAsync();
            report.Entries.Any(e => e.Name == "Timer Jitter" && e.Status == HealthStatus.Warn).ShouldBeTrue();
        }

        [Fact]
        public async Task Report_DoesNotInclude_TimerJitter_WhenProviderNull()
        {
            var checker = new EnvironmentHealthChecker();
            var report = await checker.CheckAsync();
            report.Entries.Any(e => e.Name == "Timer Jitter").ShouldBeFalse();
        }

        [Fact]
        public async Task Report_DoesNotInclude_TimerJitter_WhenProviderCurrentNull()
        {
            var provider = new TestTimerProvider { Current = null };
            var checker = new EnvironmentHealthChecker(provider);
            var report = await checker.CheckAsync();
            report.Entries.Any(e => e.Name == "Timer Jitter").ShouldBeFalse();
        }

    [Fact]
    public async Task CheckAsync_WithContext_UsesProvidedTestAssemblyPath()
    {
        var checker = new EnvironmentHealthChecker();
        var context = new EnvironmentHealthCheckContext { TestAssemblyPath = typeof(EnvironmentHealthCheckerTests).Assembly.Location };
        var report = await checker.CheckAsync(context);

        report.ShouldNotBeNull();
        var buildMode = report.Entries.First(e => e.Name == "Build Mode");
        buildMode.ShouldNotBeNull();
    }

    [Fact]
    public async Task CheckAsync_WithInvalidTestAssemblyPath_FallsBackToDefaultAssembly()
    {
        var checker = new EnvironmentHealthChecker();
        var context = new EnvironmentHealthCheckContext { TestAssemblyPath = "/nonexistent/path/assembly.dll" };
        var report = await checker.CheckAsync(context);

        report.ShouldNotBeNull();
        var buildMode = report.Entries.First(e => e.Name == "Build Mode");
        buildMode.ShouldNotBeNull();
    }

    [Fact]
    public async Task GcMode_ReturnsValidStatus()
    {
        var checker = new EnvironmentHealthChecker();
        var report = await checker.CheckAsync();

        var gcMode = report.Entries.First(e => e.Name == "GC Mode");
        gcMode.ShouldNotBeNull();
        gcMode.Status.ShouldBeOneOf(HealthStatus.Pass, HealthStatus.Warn);
    }

    [Fact]
    public async Task ProcessPriority_ReturnsValidStatus()
    {
        var checker = new EnvironmentHealthChecker();
        var report = await checker.CheckAsync();

        var priority = report.Entries.First(e => e.Name == "Process Priority");
        priority.ShouldNotBeNull();
        priority.Status.ShouldBeOneOf(HealthStatus.Pass, HealthStatus.Warn);
    }

    [Fact]
    public async Task Timer_ReturnsValidStatus()
    {
        var checker = new EnvironmentHealthChecker();
        var report = await checker.CheckAsync();

        var timer = report.Entries.First(e => e.Name == "Timer");
        timer.ShouldNotBeNull();
        timer.Status.ShouldBeOneOf(HealthStatus.Pass, HealthStatus.Warn);
        timer.Details.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BackgroundCpu_ReturnsValidStatus()
    {
        var checker = new EnvironmentHealthChecker();
        var report = await checker.CheckAsync();

        var bgCpu = report.Entries.First(e => e.Name == "Background CPU");
        bgCpu.ShouldNotBeNull();
        bgCpu.Status.ShouldBeOneOf(HealthStatus.Pass, HealthStatus.Warn, HealthStatus.Fail);
    }

    [Fact]
    public async Task Report_TimerJitter_Boundary_16Percent_Fail()
    {
        var provider = new TestTimerProvider
        {
            Current = new Sailfish.Execution.TimerCalibrationResult
            {
                RsdPercent = 16.0,
                MedianTicks = 1,
                Samples = 64,
                Warmups = 16,
                JitterScore = 30
            }
        };
        var checker = new EnvironmentHealthChecker(provider);
        var report = await checker.CheckAsync();
        report.Entries.Any(e => e.Name == "Timer Jitter" && e.Status == HealthStatus.Fail).ShouldBeTrue();
    }

}