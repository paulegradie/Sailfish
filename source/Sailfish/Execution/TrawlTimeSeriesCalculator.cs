using System;
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
    /// <param name="measuredSeconds">
    ///     The measured window's total duration in seconds. When provided (&gt; 0) and the run's final whole
    ///     second is a partial second, that last bucket's count is scaled up to a per-second rate so the
    ///     throughput series doesn't show a misleading dip at the tail. Pass 0 to bucket by raw counts only.
    /// </param>
    public static TrawlTimeSeries Compute(IReadOnlyList<RequestSample> samples, long runStartTimestamp, long frequency, double measuredSeconds = 0)
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

        // The run's final whole second is usually only partially elapsed, so its raw count understates the
        // rate — a misleading dip at the tail, which in a load test is exactly where degradation shows. When
        // the measured duration is known and its last whole second is the final populated bucket, scale that
        // bucket up to a per-second rate by the fraction of a second it actually spans.
        if (measuredSeconds > 0)
        {
            var last = count - 1;
            var fraction = measuredSeconds - offsets[last];
            if (offsets[last] == Math.Floor(measuredSeconds) && fraction > 1e-6 && fraction < 1.0)
                rps[last] /= fraction;
        }

        return new TrawlTimeSeries { SecondOffsets = offsets, RequestsPerSecond = rps, P99Ms = p99 };
    }
}
