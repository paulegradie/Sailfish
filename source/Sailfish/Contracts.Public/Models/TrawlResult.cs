using System;
using Sailfish.Trawl;

namespace Sailfish.Contracts.Public.Models;

/// <summary>
///     Summary of one Trawl (load) scenario run: how much traffic was generated, how fast it went, how
///     often it failed, and the latency distribution. All latency figures are in milliseconds (Sailfish's
///     canonical duration unit).
/// </summary>
public sealed record TrawlResult
{
    /// <summary>The scenario's display name (test case id).</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>The load model that was applied.</summary>
    public LoadModel Model { get; init; }

    /// <summary>The number of concurrent virtual users used (closed model).</summary>
    public int VirtualUsers { get; init; }

    /// <summary>The measured (post-warmup) wall-clock duration of the run.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Total completed requests in the measured window (successes + failures).</summary>
    public long TotalRequests { get; init; }

    /// <summary>Total failed requests (threw, or were reported as errors) in the measured window.</summary>
    public long TotalErrors { get; init; }

    /// <summary>Achieved throughput, requests per second, over the measured window.</summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>Error rate in the range [0, 1].</summary>
    public double ErrorRate { get; init; }

    /// <summary>Latency distribution summary (milliseconds).</summary>
    public LatencyStats Latency { get; init; } = LatencyStats.Empty;

    /// <summary>
    ///     A (possibly down-sampled) sample of per-request latencies in milliseconds, retained for the
    ///     distribution plot and for downstream statistical comparison. The summary percentiles in
    ///     <see cref="Latency" /> are computed from the full sample set; this array may be capped.
    /// </summary>
    public double[] LatencySamplesMs { get; init; } = Array.Empty<double>();

    /// <summary>Per-second throughput and tail latency over the run, or null if not captured.</summary>
    public TrawlTimeSeries? TimeSeries { get; init; }
}

/// <summary>
///     Latency distribution summary for a Trawl run, in milliseconds.
/// </summary>
public sealed record LatencyStats
{
    /// <summary>Fastest observed latency (ms).</summary>
    public double Min { get; init; }

    /// <summary>Arithmetic mean latency (ms).</summary>
    public double Mean { get; init; }

    /// <summary>Median (50th percentile) latency (ms).</summary>
    public double P50 { get; init; }

    /// <summary>75th percentile latency (ms).</summary>
    public double P75 { get; init; }

    /// <summary>90th percentile latency (ms).</summary>
    public double P90 { get; init; }

    /// <summary>95th percentile latency (ms).</summary>
    public double P95 { get; init; }

    /// <summary>99th percentile latency (ms).</summary>
    public double P99 { get; init; }

    /// <summary>Slowest observed latency (ms).</summary>
    public double Max { get; init; }

    /// <summary>An all-zero instance, used before a run has produced measurements.</summary>
    public static LatencyStats Empty => new();
}

/// <summary>
///     Per-second time-series for a Trawl run: throughput and tail latency bucketed by whole-second offset
///     from the start of the measured window.
/// </summary>
public sealed record TrawlTimeSeries
{
    /// <summary>Whole-second offsets from the start of the measured window (0, 1, 2, ...).</summary>
    public double[] SecondOffsets { get; init; } = Array.Empty<double>();

    /// <summary>Completed requests in each one-second bucket (i.e. throughput for that second).</summary>
    public double[] RequestsPerSecond { get; init; } = Array.Empty<double>();

    /// <summary>p99 latency (ms) within each one-second bucket.</summary>
    public double[] P99Ms { get; init; } = Array.Empty<double>();

    public static TrawlTimeSeries Empty => new();
}

/// <summary>
///     A persisted Trawl run: a <see cref="TrawlResult" /> plus the wall-clock timestamp it was captured at.
///     Written to the tracking directory as JSON and read back by regression analysis.
/// </summary>
public sealed record TrawlRunRecord
{
    public DateTime TimestampUtc { get; init; }
    public TrawlResult Result { get; init; } = new();
}
