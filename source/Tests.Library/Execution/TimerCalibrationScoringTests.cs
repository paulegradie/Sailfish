using System;
using System.Reflection;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class TimerCalibrationScoringTests
{
    private static int ComputeScore(double rsdPercent)
    {
        var serviceType = typeof(MarkdownTableConverter).Assembly.GetType("Sailfish.Execution.TimerCalibrationService")
                          ?? Type.GetType("Sailfish.Execution.TimerCalibrationService, Sailfish", throwOnError: true)!;
        var method = serviceType.GetMethod("ComputeJitterScoreFromRsdPercent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
        var scoreObj = method.Invoke(null, new object?[] { rsdPercent })!;
        return (int)scoreObj;
    }

    [Theory]
    [InlineData(0.0, 100)]
    [InlineData(5.0, 80)]
    [InlineData(15.0, 40)]
    [InlineData(25.0, 0)]
    [InlineData(26.0, 0)]
    [InlineData(-1.0, 100)]
    public void ComputeJitterScore_ClampsAndRounds_AsExpected(double rsd, int expected)
    {
        var score = ComputeScore(rsd);
        score.ShouldBe(expected);
    }
}