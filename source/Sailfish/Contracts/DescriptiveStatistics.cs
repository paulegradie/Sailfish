using System;
using System.Linq;
using Sailfish.Execution;

namespace Sailfish.Contracts;

public class DescriptiveStatistics
{
    public string DisplayName { get; set; } = null!;
    public double Mean { get; set; }
    public double Median { get; set; }
    public double StdDev { get; set; }
    public double Variance { get; set; }

    public double GlobalDuration { get; set; }
    public DateTimeOffset GlobalStart { get; set; }
    public DateTimeOffset GlobalEnd { get; set; }

    public double[] RawExecutionResults { get; set; } = null!; // milliseconds

    public static DescriptiveStatistics ConvertFromPerfTimer(string displayName, PerformanceTimer performanceTimer)
    {
        var executionIterations =
            performanceTimer.ExecutionIterationPerformances.Select(x => (double)x.Duration).ToArray();

        var mean = MathNet.Numerics.Statistics.Statistics.Mean(executionIterations);
        var stdDev = MathNet.Numerics.Statistics.Statistics.StandardDeviation(executionIterations);
        var variance = MathNet.Numerics.Statistics.Statistics.Variance(executionIterations);
        var median = MathNet.Numerics.Statistics.Statistics.Median(executionIterations);

        return new DescriptiveStatistics
        {
            DisplayName = displayName,
            GlobalStart = performanceTimer.GlobalStart,
            GlobalEnd = performanceTimer.GlobalStop,
            GlobalDuration = performanceTimer.GlobalDuration.TotalSeconds,
            Mean = Math.Round(mean, 5),
            StdDev = Math.Round(stdDev, 5),
            Variance = Math.Round(variance, 5),
            Median = Math.Round(median, 5),
            RawExecutionResults = executionIterations
        };
    }
}