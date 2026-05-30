using System.Threading.Tasks;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;
#pragma warning disable CS0618 // OverheadEstimator is obsolete in production but intentionally tested here

/// <summary>
/// Smoke coverage for the deprecated OverheadEstimator (retained for rollback).
/// The replacement HarnessBaselineCalibrator has its own dedicated tests; only one
/// end-to-end probe is kept here so a serious regression is still surfaced.
/// </summary>
public class OverheadEstimatorTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        var estimator = new OverheadEstimator();
        estimator.ShouldNotBeNull();
    }

    [Fact]
    public void GetAverageEstimate_WithNoEstimates_ShouldReturnZero()
    {
        var estimator = new OverheadEstimator();
        estimator.GetAverageEstimate().ShouldBe(0);
    }

    [Fact]
    public void GetAverageEstimate_ShouldClearEstimatesAfterCall()
    {
        var estimator = new OverheadEstimator();
        var firstCall = estimator.GetAverageEstimate();
        var secondCall = estimator.GetAverageEstimate();
        firstCall.ShouldBe(0);
        secondCall.ShouldBe(0);
    }

    /// <summary>
    /// Single end-to-end probe of the deprecated estimator. Estimate() runs 50
    /// 100ms Task.Delay iterations (~5s wall) so one call is enough.
    /// </summary>
    [Fact]
    public async Task Estimate_CompletesAndProducesNonNegativeAverage()
    {
        var estimator = new OverheadEstimator();
        await Should.NotThrowAsync(async () => await estimator.Estimate());
        estimator.GetAverageEstimate().ShouldBeGreaterThanOrEqualTo(0);
    }
}

#pragma warning restore CS0618
