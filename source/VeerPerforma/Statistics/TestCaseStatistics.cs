using System;
using System.Linq;
using VeerPerforma.Execution;

namespace VeerPerforma.Statistics;

public class TestCaseStatistics
{
    public string DisplayName { get; set; }
    public double Mean { get; set; }
    public double Median { get; set; }
    public double StdDev { get; set; }
    public double Variance { get; set; }

    public double GlobalDuration { get; set; }
    public DateTimeOffset GlobalStart { get; set; }
    public DateTimeOffset GlobalEnd { get; set; }

    public double[] RawExecutionResults { get; set; } // milliseconds

    public TestCaseStatistics ConvertFromPerfTimer(string displayName, PerformanceTimer performanceTimer)
    {
        var executionIterations = performanceTimer.ExecutionIterationPerformances.Select(x => (double)x.Duration).ToArray();

        var mean = MathNet.Numerics.Statistics.Statistics.Mean(executionIterations);
        var stdDev = MathNet.Numerics.Statistics.Statistics.StandardDeviation(executionIterations);
        var variance = MathNet.Numerics.Statistics.Statistics.Variance(executionIterations);
        var median = MathNet.Numerics.Statistics.Statistics.Median(executionIterations);

        return new TestCaseStatistics
        {
            DisplayName = displayName,
            GlobalStart = performanceTimer.GlobalStart,
            GlobalEnd = performanceTimer.GlobalStop,
            GlobalDuration = performanceTimer.GlobalDuration.TotalSeconds,
            Mean = Math.Round(mean, 3),
            StdDev = Math.Round(stdDev, 3),
            Variance = Math.Round(variance, 3),
            Median = Math.Round(median, 3),
            RawExecutionResults = executionIterations
        };
    }
}