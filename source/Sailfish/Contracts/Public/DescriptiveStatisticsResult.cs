using System;
using System.Linq;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public;

public class DescriptiveStatisticsResult
{
    public string DisplayName { get; init; } = null!;
    public double Mean { get; init; }
    public double Median { get; init; }
    public double StdDev { get; init; }
    public double Variance { get; init; }

    public double GlobalDuration { get; init; }
    public DateTimeOffset GlobalStart { get; init; }
    public DateTimeOffset GlobalEnd { get; init; }

    public double[] RawExecutionResults { get; init; } = null!; // milliseconds

    public static DescriptiveStatisticsResult ConvertFromPerfTimer(TestCaseId testCaseId, PerformanceTimer performanceTimer)
    {
        var executionIterations = performanceTimer.ExecutionIterationPerformances.Select(x => (double)x.Duration).ToArray();

        var mean = executionIterations.Mean();
        var stdDev = executionIterations.StandardDeviation() + 0.000000001;
        var variance = executionIterations.Variance() + 0.000000001;
        var median = executionIterations.Median();

        return new DescriptiveStatisticsResult
        {
            DisplayName = testCaseId.DisplayName,
            GlobalStart = performanceTimer.GlobalStart,
            GlobalEnd = performanceTimer.GlobalStop,
            GlobalDuration = performanceTimer.GlobalDuration.TotalSeconds,
            Mean = mean,
            StdDev = stdDev,
            Variance = variance,
            Median = median,
            RawExecutionResults = executionIterations
        };
    }
}