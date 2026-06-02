using System;
using System.IO;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class TrawlBaselineProviderTests
{
    [Fact]
    public void ReturnsMostRecentRecord_ForMatchingDisplayName()
    {
        var dir = Path.Combine(Path.GetTempPath(), "trawl_baseline_" + Guid.NewGuid().ToString("N"));
        try
        {
            var writer = new TrawlResultWriter();
            writer.PersistRecord(new TrawlResult { DisplayName = "X.Y", LatencySamplesMs = new[] { 1.0, 2 }, RequestsPerSecond = 10 }, DateTime.UtcNow.AddMinutes(-10), dir);
            writer.PersistRecord(new TrawlResult { DisplayName = "X.Y", LatencySamplesMs = new[] { 3.0, 4 }, RequestsPerSecond = 20 }, DateTime.UtcNow.AddMinutes(-1), dir);
            writer.PersistRecord(new TrawlResult { DisplayName = "Other.Z", LatencySamplesMs = new[] { 9.0 }, RequestsPerSecond = 99 }, DateTime.UtcNow, dir);

            var baseline = new TrawlBaselineProvider().GetLatestBaseline("X.Y", dir);

            baseline.ShouldNotBeNull();
            baseline!.Result.RequestsPerSecond.ShouldBe(20); // the most recent X.Y record, not the Other.Z one
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void ReturnsNull_WhenNoMatchingScenario()
    {
        var dir = Path.Combine(Path.GetTempPath(), "trawl_baseline_none_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            new TrawlBaselineProvider().GetLatestBaseline("Nope", dir).ShouldBeNull();
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }
}
