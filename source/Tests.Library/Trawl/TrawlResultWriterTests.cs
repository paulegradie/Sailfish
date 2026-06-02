using System;
using System.IO;
using System.Text.Json;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Trawl;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class TrawlResultWriterTests
{
    [Fact]
    public void PersistRecord_RoundTripsThroughJson_AndWritesReport()
    {
        var result = new TrawlResult
        {
            DisplayName = "My.Test(x: 1)",
            Model = LoadModel.ClosedModel,
            VirtualUsers = 4,
            Duration = TimeSpan.FromSeconds(2),
            TotalRequests = 100,
            TotalErrors = 2,
            RequestsPerSecond = 50,
            ErrorRate = 0.02,
            Latency = new LatencyStats { Min = 1, Mean = 6, P50 = 5, P99 = 20, Max = 25 },
            LatencySamplesMs = new[] { 1.0, 2, 3 },
            TimeSeries = new TrawlTimeSeries
            {
                SecondOffsets = new double[] { 0, 1 },
                RequestsPerSecond = new double[] { 50, 50 },
                P99Ms = new double[] { 20, 21 }
            }
        };

        var dir = Path.Combine(Path.GetTempPath(), "trawl_writer_" + Guid.NewGuid().ToString("N"));
        try
        {
            var writer = new TrawlResultWriter();
            var timestamp = DateTime.UtcNow;

            var jsonPath = writer.PersistRecord(result, timestamp, dir);
            var mdPath = writer.WriteReport("# report body", result, timestamp, dir);

            File.Exists(jsonPath).ShouldBeTrue();
            File.Exists(mdPath).ShouldBeTrue();
            jsonPath.ShouldContain(Path.Combine("trawl", "My.Test")); // sanitized into the trawl subdir

            var record = JsonSerializer.Deserialize<TrawlRunRecord>(File.ReadAllText(jsonPath));
            record.ShouldNotBeNull();
            record!.Result.DisplayName.ShouldBe("My.Test(x: 1)");
            record.Result.Model.ShouldBe(LoadModel.ClosedModel);
            record.Result.RequestsPerSecond.ShouldBe(50);
            record.Result.Latency.P99.ShouldBe(20);
            record.Result.LatencySamplesMs.Length.ShouldBe(3);
            record.Result.TimeSeries.ShouldNotBeNull();
            record.Result.TimeSeries!.RequestsPerSecond.Length.ShouldBe(2);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }
}
