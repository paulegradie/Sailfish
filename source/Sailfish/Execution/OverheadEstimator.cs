using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace Sailfish.Execution;

[Obsolete("Deprecated. Use HarnessBaselineCalibrator for overhead calibration. Retained for rollback safety.")]
public class OverheadEstimator
{
    private const double NumMilliSecondsToWait = 100;

    private readonly List<double> _estimates = new();
    private static double TicksPerMillisecond => Stopwatch.Frequency / (double)1_000;
    private static double ExpectedWaitPeriodInTicks => TicksPerMillisecond * NumMilliSecondsToWait;

    public int GetAverageEstimate()
    {
        var result = _estimates.Count > 0 ? (int)_estimates.Mean() : 0;
        _estimates.Clear();
        return result;
    }

    public async Task Estimate()
    {
        var method = typeof(OverheadEstimator).GetMethod(nameof(Wait));

        var totalElapsedTicks = new List<double>();

        for (var i = 0; i < 30; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            // Invoke the method using reflection
            await (Task)method?.Invoke(this, null)!;

            stopwatch.Stop();
            totalElapsedTicks.Add(stopwatch.ElapsedTicks);
        }

        var averageElapsedTicks = totalElapsedTicks.Median();
        var overheadInAverageTicks = averageElapsedTicks - ExpectedWaitPeriodInTicks;

        // do a second estimation
        var followupEstimate = new List<double>();
        for (var i = 0; i < 20; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            // Invoke the method using reflection
            await (Task)method?.Invoke(this, null)!;

            stopwatch.Stop();
            followupEstimate.Add(stopwatch.ElapsedTicks);
        }

        var meanFollowupTicks = followupEstimate.Median();

        if (overheadInAverageTicks < 0) return;
        while (meanFollowupTicks - overheadInAverageTicks < ExpectedWaitPeriodInTicks)
        {
            overheadInAverageTicks -= 100;
            if (overheadInAverageTicks <= 0) break;
        }

        if (overheadInAverageTicks < 0) return;
        var estimate = (int)Math.Round(overheadInAverageTicks * 0.25, 0);
        _estimates.Add(estimate);
    }

#pragma warning disable CA1822

    public async Task Wait()
#pragma warning restore CA1822
    {
        await Task.Delay((int)NumMilliSecondsToWait);
    }
}