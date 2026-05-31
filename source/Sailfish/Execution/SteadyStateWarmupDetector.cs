using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace Sailfish.Execution;

/// <summary>
///     Outcome of a steady-state warmup check.
/// </summary>
public sealed class SteadyStateWarmupResult
{
    public bool ReachedSteadyState { get; init; }
    public double RelativeDrift { get; init; }
    public double CoefficientOfVariation { get; init; }
    public int WindowSize { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
///     Detects when warmup measurements have reached steady state — i.e. the per-iteration duration has
///     stopped trending (tiered JIT compilation/OSR has settled) and is now stable.
///     <para>
///         This is distinct from sampling convergence (<see cref="Sailfish.Analysis.StatisticalConvergenceDetector" />),
///         which only looks at dispersion. Warmup needs to detect that the central tendency has stopped
///         <i>moving</i>: durations start high and fall as the runtime tiers up, then flatten. So this
///         compares the median of the most recent half of a sliding window against the prior half (the
///         trend), and additionally requires the recent window's dispersion (CV) to be low. Medians are
///         used so a single cold-start spike doesn't dominate the decision.
///     </para>
/// </summary>
public sealed class SteadyStateWarmupDetector
{
    /// <summary>
    ///     Returns whether the tail of <paramref name="warmupDurations" /> looks steady. A decision is only
    ///     made once at least <paramref name="window" /> measurements are available.
    /// </summary>
    /// <param name="warmupDurations">Per-iteration warmup durations in chronological order (any consistent unit).</param>
    /// <param name="window">Number of most-recent measurements to examine (split into prior/recent halves).</param>
    /// <param name="maxRelativeDrift">Max allowed |recentMedian - priorMedian| / priorMedian to be considered "no longer trending".</param>
    /// <param name="maxCoefficientOfVariation">Max allowed CV over the recent window to be considered "stable".</param>
    public SteadyStateWarmupResult Check(
        IReadOnlyList<double> warmupDurations,
        int window,
        double maxRelativeDrift,
        double maxCoefficientOfVariation)
    {
        if (warmupDurations is null || window < 2)
            return new SteadyStateWarmupResult { WindowSize = window, Reason = "insufficient configuration" };

        var n = warmupDurations.Count;
        if (n < window)
            return new SteadyStateWarmupResult { WindowSize = window, Reason = $"need {window} samples, have {n}" };

        var half = window / 2;
        var recent = new double[half];
        var prior = new double[half];
        for (var i = 0; i < half; i++)
        {
            recent[i] = warmupDurations[n - 1 - i];        // newest `half`
            prior[i] = warmupDurations[n - 1 - half - i];  // the `half` immediately before
        }

        var recentMedian = recent.Median();
        var priorMedian = prior.Median();
        var relativeDrift = Math.Abs(priorMedian) <= 1e-9 ? 0.0 : Math.Abs(recentMedian - priorMedian) / Math.Abs(priorMedian);

        var win = new double[window];
        for (var i = 0; i < window; i++) win[i] = warmupDurations[n - window + i];
        var mean = win.Mean();
        var cv = Math.Abs(mean) <= 1e-9 ? 0.0 : Math.Abs(win.StandardDeviation() / mean);

        var steady = relativeDrift <= maxRelativeDrift && cv <= maxCoefficientOfVariation;
        return new SteadyStateWarmupResult
        {
            ReachedSteadyState = steady,
            RelativeDrift = relativeDrift,
            CoefficientOfVariation = cv,
            WindowSize = window,
            Reason = steady
                ? $"drift {relativeDrift:F3} <= {maxRelativeDrift:F3}, CV {cv:F3} <= {maxCoefficientOfVariation:F3}"
                : $"not steady: drift {relativeDrift:F3} (max {maxRelativeDrift:F3}), CV {cv:F3} (max {maxCoefficientOfVariation:F3})"
        };
    }
}
