using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Diagnostics.Environment;
using Sailfish.TestAdapter.Execution.EnvironmentHealth;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.EnvironmentHealth;

public class EnvironmentHealthCheckRunnerTests
{
    private sealed class StubChecker : IEnvironmentHealthChecker
    {
        private readonly EnvironmentHealthReport report;
        public StubChecker(EnvironmentHealthReport report) => this.report = report;
        public Task<EnvironmentHealthReport> CheckAsync(EnvironmentHealthCheckContext? context = null, CancellationToken cancellationToken = default)
            => Task.FromResult(report);
    }

    [Fact]
    public async Task RunAsync_Returns_Report_And_Summary()
    {
        var entries = new List<HealthCheckEntry>
        {
            new("Process Priority", HealthStatus.Pass, "High"),
            new("GC Mode", HealthStatus.Warn, "Workstation"),
        };
        var stubReport = new EnvironmentHealthReport(entries);
        var runner = new EnvironmentHealthCheckRunner(new StubChecker(stubReport));

        var (report, summary) = await runner.RunAsync(null, CancellationToken.None);
        report.ShouldBeSameAs(stubReport);
        summary.ShouldContain("Sailfish Environment Health:");
        summary.ShouldContain("Process Priority");
        summary.ShouldContain("GC Mode");
    }

    [Fact]
    public async Task RunAndFormatSummaryAsync_Works()
    {
        var entries = new List<HealthCheckEntry>
        {
            new("Timer", HealthStatus.Pass, "High-resolution"),
        };
        var stubReport = new EnvironmentHealthReport(entries);
        var runner = new EnvironmentHealthCheckRunner(new StubChecker(stubReport));

        var summary = await runner.RunAndFormatSummaryAsync(null, CancellationToken.None);
        summary.ShouldContain("Timer");
    }
}

