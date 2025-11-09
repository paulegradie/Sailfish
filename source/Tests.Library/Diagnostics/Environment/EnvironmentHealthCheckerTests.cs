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

}
