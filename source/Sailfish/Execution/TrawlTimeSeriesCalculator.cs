using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

/// <summary>
///     Buckets a run's request samples into whole-second windows (from the start of the measured window) to
///     produce a throughput-and-tail-latency time-series. Pure post-processing over the samples the
///     scheduler already captured — no extra cost during the run.
/// </summary>
internal static class TrawlTimeSeriesCalculator
{
    public static TrawlTimeSeries Compute(IReadOnlyList<RequestSample> samples, long runStartTimestamp, long frequency)
    {
        if (samples is null || samples.Count == 0 || frequency <= 0) return TrawlTimeSeries.Empty;

        var buckets = new SortedDictionary<long, List<double>>();
        foreach (var sample in samples)
        {
            var offsetSeconds = (long)((double)(sample.StartTimestamp - runStartTimestamp) / frequency);
            if (offsetSeconds < 0) offsetSeconds = 0;
            if (!buckets.TryGetValue(offsetSeconds, out var latencies))
            {
                latencies = new List<double>();
                buckets[offsetSeconds] = latencies;
            }

            latencies.Add(sample.LatencyTicks * 1000.0 / frequency);
        }

        var count = buckets.Count;
        var offsets = new double[count];
        var rps = new double[count];
        var p99 = new double[count];

        var i = 0;
        foreach (var bucket in buckets)
        {
            offsets[i] = bucket.Key;
            rps[i] = bucket.Value.Count; // a one-second bucket, so the count is requests/second
            p99[i] = LatencyStatsCalculator.Compute(bucket.Value).P99;
            i++;
        }

        return new TrawlTimeSeries { SecondOffsets = offsets, RequestsPerSecond = rps, P99Ms = p99 };
    }
}
