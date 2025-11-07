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
}

