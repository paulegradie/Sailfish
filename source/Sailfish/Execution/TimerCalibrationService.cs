using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

public interface ITimerCalibrationService
{
    Task<TimerCalibrationResult> CalibrateAsync(CancellationToken cancellationToken = default);
}

public sealed class TimerCalibrationResult
{
    public long StopwatchFrequency { get; init; }
    public double ResolutionNs { get; init; }
    public int BaselineOverheadTicks { get; init; }
    public int Warmups { get; init; }
    public int Samples { get; init; }
    public double StdDevTicks { get; init; }
    public long MedianTicks { get; init; }
    public double RsdPercent { get; init; }
    public int JitterScore { get; init; }
}

public interface ITimerCalibrationResultProvider
{
    TimerCalibrationResult? Current { get; set; }
}

internal sealed class TimerCalibrationResultProvider : ITimerCalibrationResultProvider
{
    public TimerCalibrationResult? Current { get; set; }
}

internal sealed class TimerCalibrationService : ITimerCalibrationService
{
    private const int WarmupCount = 16;
    private const int SampleCount = 64;

    public Task<TimerCalibrationResult> CalibrateAsync(CancellationToken cancellationToken = default)
    {
        // Use a fixed sync no-op probe to keep results comparable across sessions
        var probe = typeof(CalibrationProbes).GetMethod("SyncNoop", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    ?? throw new InvalidOperationException("Calibration probe 'SyncNoop' not found");

        // Create a fast delegate to the sync no-op
        var action = (Action)Delegate.CreateDelegate(typeof(Action), probe);

        // Warmup (JIT/infra)
        for (var i = 0; i < WarmupCount; i++)
        {
            action();
        }

        // Measure N samples (outer Stopwatch includes delegate invoke + timer start/stop cost)
        var samples = new List<long>(SampleCount);
        for (var i = 0; i < SampleCount; i++)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            samples.Add(sw.ElapsedTicks);
        }

        if (samples.Count == 0)
        {
            return Task.FromResult(BuildResult(0, 0, 0.0, 0));
        }

        // Compute median and stddev over raw ticks
        var ordered = samples.OrderBy(v => v).ToArray();
        var median = (ordered.Length % 2 == 1) ? ordered[ordered.Length / 2] :
            ordered[(ordered.Length / 2) - 1] + ((ordered[ordered.Length / 2] - ordered[(ordered.Length / 2) - 1]) / 2);

        // stddev
        var mean = samples.Average();
        var variance = 0.0;
        if (samples.Count > 1)
        {
            var diffsq = samples.Select(v => (v - mean) * (v - mean)).Sum();
            variance = diffsq / (samples.Count - 1);
        }
        var stddev = Math.Sqrt(Math.Max(0.0, variance));

        // RSD% relative to mean (avoid div-by-zero)
        var rsdPct = (mean > 0.0) ? (stddev / mean) * 100.0 : 100.0;
        // Score: higher is better, clamp 0..100
        var score = (int)Math.Round(Math.Clamp(100.0 - (rsdPct * 4.0), 0.0, 100.0));

        // Clamp median to int range non-negative
        if (median < 0) median = 0;
        if (median > int.MaxValue) median = int.MaxValue;

        return Task.FromResult(BuildResult((int)median, stddev, rsdPct, score));
    }


    // Exposed for tests via reflection to validate scoring independently of sampling noise
    internal static int ComputeJitterScoreFromRsdPercent(double rsdPercent)
    {
        return (int)Math.Round(Math.Clamp(100.0 - (rsdPercent * 4.0), 0.0, 100.0));
    }

    private static TimerCalibrationResult BuildResult(int baselineTicks, double stdDevTicks, double rsdPercent, int jitterScore)
    {
        var freq = Stopwatch.Frequency;
        var resNs = 1_000_000_000.0 / Math.Max(1, freq);
        return new TimerCalibrationResult
        {
            StopwatchFrequency = freq,
            ResolutionNs = resNs,
            BaselineOverheadTicks = baselineTicks,
            Warmups = WarmupCount,
            Samples = SampleCount,
            StdDevTicks = stdDevTicks,
            MedianTicks = baselineTicks,
            RsdPercent = rsdPercent,
            JitterScore = jitterScore
        };
    }
}