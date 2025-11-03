using System;
using System.Diagnostics;
using System.Linq;
using Shouldly;
using Xunit;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Tests.Library.Contracts;

public class PerformanceRunResultTests
{
    [Fact]
    public void ComputeConfidenceIntervals_ReturnsZeroMargins_WhenNLessOrEqualToOne()
    {
        // Arrange
        var mean = 123.45;
        var se = 0.0; // SE=0 simulates n<=1 or no variation
        var n = 1;

        // Act
        var cis = PerformanceRunResult.ComputeConfidenceIntervals(mean, se, n, new[] {0.95, 0.99});

        // Assert
        cis.Count.ShouldBe(2);
        cis.All(ci => Math.Abs(ci.MarginOfError) < 1e-12).ShouldBeTrue();
        cis.All(ci => Math.Abs(ci.Lower - mean) < 1e-12 && Math.Abs(ci.Upper - mean) < 1e-12).ShouldBeTrue();
    }

    [Fact]
    public void ComputeConfidenceIntervals_ReturnsMonotonicMargins_ForMultipleLevels()
    {
        // Arrange: n>1 and SE>0
        var mean = 50.0;
        var se = 1.0;
        var n = 10;

        // Act
        var cis = PerformanceRunResult.ComputeConfidenceIntervals(mean, se, n, new[] {0.95, 0.99});

        // Assert: 99% CI should have larger margin than 95%
        var ci95 = cis.Single(ci => Math.Abs(ci.ConfidenceLevel - 0.95) < 1e-9);
        var ci99 = cis.Single(ci => Math.Abs(ci.ConfidenceLevel - 0.99) < 1e-9);
        ci99.MarginOfError.ShouldBeGreaterThan(ci95.MarginOfError);
        ci99.Lower.ShouldBeLessThan(ci95.Lower);
        ci95.Upper.ShouldBeLessThan(ci99.Upper);
    }

    [Fact]
    public void ConvertFromPerfTimer_WithIdenticalTicks_ProducesZeroStdDev_AndCorrectMean()
    {
        // Arrange
        var freq = Stopwatch.Frequency; // ticks per second
        var oneSecondTicks = freq; // => 1000 ms
        var timer = new PerformanceTimer();
        timer.ExecutionIterationPerformances.Add(new IterationPerformance(DateTimeOffset.Now, DateTimeOffset.Now, oneSecondTicks));
        timer.ExecutionIterationPerformances.Add(new IterationPerformance(DateTimeOffset.Now, DateTimeOffset.Now, oneSecondTicks));

        var exec = new ExecutionSettings(asCsv: false, asConsole: true, asMarkdown: false, sampleSize: 2, numWarmupIterations: 0);
        var testCaseId = new TestCaseId(new TestCaseName(new[] {"C","M"}), new TestCaseVariables([]));

        // Act
        var perf = PerformanceRunResult.ConvertFromPerfTimer(testCaseId, timer, exec);

        // Assert
        perf.SampleSize.ShouldBe(2);
        perf.NumWarmupIterations.ShouldBe(0);
        System.Math.Abs(perf.Mean - 1000.0).ShouldBeLessThan(1e-6);
        System.Math.Abs(perf.Median - 1000.0).ShouldBeLessThan(1e-6);
        System.Math.Abs(perf.StdDev - 0.0).ShouldBeLessThan(1e-9);
        System.Math.Abs(perf.Variance - 0.0).ShouldBeLessThan(1e-9);
        perf.ConfidenceIntervals.ShouldNotBeNull();
    }
}

