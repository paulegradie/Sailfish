using System;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies that the SuggestedNextN diagnostic appears (and only appears) when the classification is not
/// distinguishable, and that the value is a sensible "extend the probe" recommendation.
/// </summary>
public class ScaleFishSuggestionTests
{
    [Fact]
    public void Distinguishable_NoSuggestion()
    {
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 1024, 6));
        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.IsDistinguishable.ShouldBeTrue();
        result.SuggestedNextN.ShouldBeNull("no suggestion needed when the result is already distinguishable");
    }

    [Fact]
    public void Undistinguishable_SuggestsAtLeastDoubleMaxX()
    {
        // Force a near-tie by giving the estimator only two points that satisfy both Linear and NLogN
        // perfectly (any line through them has the same SSD = 0 under either basis). The estimator
        // returns a model but cannot confidently rank — distinguishability flips to false.
        var measurements = new[]
        {
            new ComplexityMeasurement(2, 4),
            new ComplexityMeasurement(4, 8)
        };
        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.IsDistinguishable.ShouldBeFalse("two points don't admit AICc small-sample correction");
        result.SuggestedNextN.ShouldNotBeNull();
        result.SuggestedNextN.Value.ShouldBeGreaterThanOrEqualTo(8); // at least 2 * maxX (4)
    }
}
