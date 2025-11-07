using System.Collections.Generic;
using Sailfish.Diagnostics.Environment;
using Shouldly;
using Xunit;

namespace Tests.Library.Diagnostics.Environment;

public class EnvironmentHealthReportTests
{
    [Fact]
    public void Score_Is_100_When_All_Pass()
    {
        var entries = new List<HealthCheckEntry>
        {
            new("A", HealthStatus.Pass, "ok"),
            new("B", HealthStatus.Pass, "ok"),
            new("C", HealthStatus.Pass, "ok"),
            new("D", HealthStatus.Pass, "ok"),
            new("E", HealthStatus.Pass, "ok"),
        };

        var report = new EnvironmentHealthReport(entries);
        report.Score.ShouldBe(100);
        report.SummaryLabel.ShouldBe("Excellent");
    }

    [Fact]
    public void Score_Computes_Correctly_For_Mixed_Statuses()
    {
        // Pass=10, Warn=6, Fail=0, Unknown=4
        var entries = new List<HealthCheckEntry>
        {
            new("P1", HealthStatus.Pass, ""),
            new("P2", HealthStatus.Pass, ""),
            new("W1", HealthStatus.Warn, ""),
            new("U1", HealthStatus.Unknown, ""),
            new("F1", HealthStatus.Fail, ""),
        };
        // total = 10 + 10 + 6 + 4 + 0 = 30; max=50 => 60%
        var report = new EnvironmentHealthReport(entries);
        report.Score.ShouldBe(60);
        report.SummaryLabel.ShouldBe("Fair");
    }

    [Fact]
    public void Empty_Entries_Yield_Zero_Score_And_Poor_Label()
    {
        var report = new EnvironmentHealthReport(new List<HealthCheckEntry>());
        report.Score.ShouldBe(0);
        report.SummaryLabel.ShouldBe("Poor");
    }
}

