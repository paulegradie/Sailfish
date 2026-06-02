using System;
using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

/// <summary>
///     Computes a <see cref="LatencyStats" /> summary (milliseconds) from raw latency samples. Unlike the
///     microbenchmark path, Trawl does <b>not</b> remove outliers — the slow tail is exactly what a load test
///     is trying to surface, so p95/p99 must include it. Percentiles use the nearest-rank method.
/// </summary>
internal static class LatencyStatsCalculator
{
    public static LatencyStats Compute(IReadOnlyList<double> latenciesMs)
    {
        if (latenciesMs is null || latenciesMs.Count == 0) return LatencyStats.Empty;

        var sorted = new double[latenciesMs.Count];
        for (var i = 0; i < latenciesMs.Count; i++) sorted[i] = latenciesMs[i];
        Array.Sort(sorted);

        double sum = 0;
        foreach (var value in sorted) sum += value;

        return new LatencyStats
        {
            Min = sorted[0],
            Max = sorted[sorted.Length - 1],
            Mean = sum / sorted.Length,
            P50 = Percentile(sorted, 0.50),
            P75 = Percentile(sorted, 0.75),
            P90 = Percentile(sorted, 0.90),
            P95 = Percentile(sorted, 0.95),
            P99 = Percentile(sorted, 0.99)
        };
    }

    /// <summary>Nearest-rank percentile on an ascending-sorted array. <paramref name="p" /> is in (0, 1].</summary>
    private static double Percentile(double[] sortedAscending, double p)
    {
        var n = sortedAscending.Length;
        if (n == 1) return sortedAscending[0];

        var rank = (int)Math.Ceiling(p * n);
        if (rank < 1) rank = 1;
        if (rank > n) rank = n;
        return sortedAscending[rank - 1];
    }
}
