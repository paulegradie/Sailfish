using System;
using System.Linq;
using Accord.Statistics;
using Sailfish.Analysis;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public;

public class DescriptiveStatisticsResult
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

    public static DescriptiveStatisticsResult ConvertFromPerfTimer(TestCaseId testCaseId, PerformanceTimer performanceTimer)
    {
        var executionIterations = performanceTimer.ExecutionIterationPerformances.Select(x => (double)x.Duration).ToArray();

        var mean = executionIterations.Mean();
        var stdDev = executionIterations.StandardDeviation();
        var variance = executionIterations.Variance();
        var median = executionIterations.Median();

        return new DescriptiveStatisticsResult
        {
            DisplayName = testCaseId.DisplayName,
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