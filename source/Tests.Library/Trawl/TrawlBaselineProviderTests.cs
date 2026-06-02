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

    [Fact]
    public void DoesNotConfuse_ScenariosThatShareANamePrefix()
    {
        var dir = Path.Combine(Path.GetTempPath(), "trawl_baseline_prefix_" + Guid.NewGuid().ToString("N"));
        try
        {
            var writer = new TrawlResultWriter();
            // "Checkout" is a string-prefix of "CheckoutFast"; the "_" separator in the filename stem must
            // keep their baselines distinct even though prefix-filtering drives the (efficient) lookup. The
            // CheckoutFast record is newer, so a naive prefix match would wrongly return it for "Checkout".
            writer.PersistRecord(new TrawlResult { DisplayName = "Checkout", RequestsPerSecond = 1, LatencySamplesMs = new[] { 1.0 } }, DateTime.UtcNow.AddMinutes(-5), dir);
            writer.PersistRecord(new TrawlResult { DisplayName = "CheckoutFast", RequestsPerSecond = 999, LatencySamplesMs = new[] { 2.0 } }, DateTime.UtcNow, dir);

            var baseline = new TrawlBaselineProvider().GetLatestBaseline("Checkout", dir);

            baseline.ShouldNotBeNull();
            baseline!.Result.DisplayName.ShouldBe("Checkout");
            baseline.Result.RequestsPerSecond.ShouldBe(1); // not the newer CheckoutFast record
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }
}
