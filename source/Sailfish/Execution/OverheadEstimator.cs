using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace Sailfish.Execution;

public class OverheadEstimator
{
    private const double NumMilliSecondsToWait = 100;
    private static double TicksPerMillisecond => Stopwatch.Frequency / (double)1_000;
    private static double ExpectedWaitPeriodInTicks => TicksPerMillisecond * (double)NumMilliSecondsToWait;

    private readonly List<double> estimates = new();

    public int GetAverageEstimate()
    {
        var result = estimates.Count > 0 ? (int)estimates.Mean() : 0;
        estimates.Clear();
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

        var averageElapsedTicks = totalElapsedTicks.Mean();
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

        var meanFollowupTicks = followupEstimate.Mean();

        if (overheadInAverageTicks < 0) return;
        while (meanFollowupTicks - overheadInAverageTicks < ExpectedWaitPeriodInTicks)
        {
            overheadInAverageTicks -= 100;
            if (overheadInAverageTicks <= 0) break;
        }

        if (overheadInAverageTicks < 0) return;
        var estimate = (int)Math.Round(overheadInAverageTicks * 0.25, 0);
        estimates.Add(estimate);
    }

#pragma warning disable CA1822
    public async Task Wait()
#pragma warning restore CA1822
    {
        await Task.Delay((int)NumMilliSecondsToWait);
    }
}