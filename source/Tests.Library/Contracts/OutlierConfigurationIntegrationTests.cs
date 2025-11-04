using System;
using System.Diagnostics;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Analysis;
using Shouldly;
using Xunit;

namespace Tests.Library.Contracts;

public class OutlierConfigurationIntegrationTests
{
    private static long TicksForMs(double ms) => (long)Math.Round(Stopwatch.Frequency * (ms / 1000.0));

    private static PerformanceTimer TimerFromMs(params double[] ms)
    {
        var timer = new PerformanceTimer();
        foreach (var m in ms)
        {
            var ticks = TicksForMs(m);
            timer.ExecutionIterationPerformances.Add(new IterationPerformance(DateTimeOffset.Now, DateTimeOffset.Now, ticks));
        }
        return timer;
    }

    private static TestCaseId MakeTestId() => new(new TestCaseName(["C","M"]), new TestCaseVariables([]));

    [Fact]
    public void Legacy_RemoveAll_Is_Default_When_OptIn_Flag_Is_False()
    {
        // Arrange: baseline cluster around 10ms, with clear lower and upper outliers
        var timer = TimerFromMs(0.0, 9.0, 10.0, 10.5, 11.0, 1000.0);
        var settings = new ExecutionSettings(asCsv: false, asConsole: false, asMarkdown: false, sampleSize: 6, numWarmupIterations: 0)
        {
            UseConfigurableOutlierDetection = false // legacy path -> RemoveAll
        };

        // Act
        var pr = PerformanceRunResult.ConvertFromPerfTimer(MakeTestId(), timer, settings);

        // Assert: both extremes removed
        pr.DataWithOutliersRemoved.ShouldNotContain(0.0);
        pr.DataWithOutliersRemoved.ShouldNotContain(1000.0);
        pr.LowerOutliers.ShouldContain(0.0);
        pr.UpperOutliers.ShouldContain(1000.0);
    }

    [Fact]
    public void Configurable_RemoveUpper_Removes_Only_Upper_Outliers()
    {
        // Arrange
        var timer = TimerFromMs(0.0, 9.0, 10.0, 10.5, 11.0, 1000.0);
        var settings = new ExecutionSettings(asCsv: false, asConsole: false, asMarkdown: false, sampleSize: 6, numWarmupIterations: 0)
        {
            UseConfigurableOutlierDetection = true,
            OutlierStrategy = OutlierStrategy.RemoveUpper
        };

        // Act
        var pr = PerformanceRunResult.ConvertFromPerfTimer(MakeTestId(), timer, settings);

        // Assert: upper removed, lower retained
        pr.DataWithOutliersRemoved.ShouldContain(0.0);
        pr.DataWithOutliersRemoved.ShouldNotContain(1000.0);
        pr.UpperOutliers.ShouldContain(1000.0);
        pr.LowerOutliers.ShouldContain(0.0);
    }

    [Fact]
    public void Configurable_DontRemove_Keeps_All_Data()
    {
        // Arrange
        var timer = TimerFromMs(0.0, 9.0, 10.0, 10.5, 11.0, 1000.0);
        var settings = new ExecutionSettings(asCsv: false, asConsole: false, asMarkdown: false, sampleSize: 6, numWarmupIterations: 0)
        {
            UseConfigurableOutlierDetection = true,
            OutlierStrategy = OutlierStrategy.DontRemove
        };

        // Act
        var pr = PerformanceRunResult.ConvertFromPerfTimer(MakeTestId(), timer, settings);

        // Assert: all original points retained
        pr.DataWithOutliersRemoved.ShouldContain(0.0);
        pr.DataWithOutliersRemoved.ShouldContain(9.0);
        pr.DataWithOutliersRemoved.ShouldContain(10.0);
        pr.DataWithOutliersRemoved.ShouldContain(10.5);
        pr.DataWithOutliersRemoved.ShouldContain(11.0);
        pr.DataWithOutliersRemoved.ShouldContain(1000.0);
    }
}

