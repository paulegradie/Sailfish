using System;

namespace Sailfish.Execution;

/// <summary>
/// Budget-aware controller that relaxes precision targets (CV/CI) based on remaining time budget.
/// Opt-in via ExecutionSettings.UseTimeBudgetController.
/// </summary>
internal sealed class PrecisionTimeBudgetController
{
    /// <summary>
    /// Compute adjusted thresholds using a simple budget heuristic. Returns original thresholds when disabled or no budget.
    /// </summary>
    /// <param name="pilotSamplesNanoSeconds">Pilot samples in nanoseconds (minimum phase).</param>
    /// <param name="settings">Execution settings containing thresholds and time budget.</param>
    /// <param name="testStart">Test start time for computing elapsed.</param>
    /// <param name="now">Current time.</param>
    public (double TargetCV, double MaxConfidenceIntervalWidth, double RemainingMs, double PerIterMs, int AllowedIterations) Adjust(
        double[] pilotSamplesNanoSeconds,
        IExecutionSettings settings,
        DateTimeOffset testStart,
        DateTimeOffset now)
    {
        var targetCv = settings.TargetCoefficientOfVariation;
        var maxCi = settings.MaxConfidenceIntervalWidth;

        if (!settings.UseTimeBudgetController || !settings.MaxMeasurementTimePerMethod.HasValue)
        {
            return (targetCv, maxCi, 0, 0, int.MaxValue);
        }

        var remaining = settings.MaxMeasurementTimePerMethod.Value - (now - testStart);
        var remainingMs = remaining.TotalMilliseconds;
        if (remainingMs <= 0)
        {
            return (targetCv, maxCi, 0, 0, 0);
        }

        var perIterMs = Math.Max(0.001, Median(pilotSamplesNanoSeconds) / 1_000_000.0);
        var allowed = (int)Math.Floor(remainingMs / perIterMs);

        double factor = 1.0;
        if (allowed <= 1) factor = 2.0;
        else if (allowed <= 3) factor = 1.5;
        else if (allowed <= 5) factor = 1.25;

        if (factor > 1.0)
        {
            // Relax thresholds conservatively within caps
            targetCv = Math.Min(0.20, Math.Max(targetCv, targetCv * factor));
            maxCi = Math.Min(0.50, Math.Max(maxCi, maxCi * factor));
        }

        return (targetCv, maxCi, Math.Max(0, remainingMs), perIterMs, allowed);
    }

    private static double Median(double[] xs)
    {
        if (xs == null || xs.Length == 0) return 0;
        var arr = (double[])xs.Clone();
        Array.Sort(arr);
        int mid = arr.Length / 2;
        if ((arr.Length & 1) == 1) return arr[mid];
        return 0.5 * (arr[mid - 1] + arr[mid]);
    }
}

