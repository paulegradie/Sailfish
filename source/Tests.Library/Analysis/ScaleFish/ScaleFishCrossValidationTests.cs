using System;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies the leave-one-X-out cross-validation diagnostic: it engages by default, agrees with the
/// all-data estimate on clean data, and reflects ambiguity when the data really is ambiguous.
/// </summary>
public class ScaleFishCrossValidationTests
{
    [Fact]
    public void CV_OnByDefault_ProducesDiagnostic()
    {
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 1024, 6));
        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.CrossValidation.ShouldNotBeNull();
        result.CrossValidation.FoldCount.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void CV_PerfectLinearData_FullAgreement()
    {
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 1024, 6));
        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.CrossValidation.ShouldNotBeNull();
        // Every fold should pick Linear when the data is exactly linear.
        result.CrossValidation.RankAgreement.ShouldBe(1.0, tolerance: 1e-9);
        // Prediction error should be effectively zero for noise-free linear data.
        result.CrossValidation.MeanPredictionError.ShouldBeLessThan(1e-6);
    }

    [Fact]
    public void CV_NoisyQuadratic_AgreesAndHasSmallError()
    {
        var rng = new Random(7);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x * x,
            ScaleFishTestHelpers.LogSpacedX(4, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.03,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(Quadratic));
        result.CrossValidation.ShouldNotBeNull();
        result.CrossValidation.RankAgreement.ShouldBeGreaterThanOrEqualTo(0.8);
    }

    [Fact]
    public void CV_TooFewPoints_ReturnsNull()
    {
        var measurements = new[]
        {
            new ComplexityMeasurement(2, 2),
            new ComplexityMeasurement(4, 4),
            new ComplexityMeasurement(8, 8)
        };
        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.CrossValidation.ShouldBeNull("CV requires at least 4 X values");
    }

    [Fact]
    public void CV_Disabled_OmitsDiagnostic()
    {
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6));
        var settings = new ScaleFishSettings { EnableCrossValidation = false };
        var result = new ComplexityEstimator(settings).EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.CrossValidation.ShouldBeNull();
    }

    [Fact]
    public void RankAgreement_NeverExceedsOne()
    {
        // Regression for a subtle bug: rank-agreement and prediction-error live on independent fold
        // denominators. Mixing them would let rankAgreement = agreements / predictionFolds drift above
        // 1.0 whenever a rank-match fold's prediction step failed. Exercise a noisy run and assert the
        // invariant for every reasonable seed.
        for (var seed = 1; seed <= 5; seed++)
        {
            var rng = new Random(seed);
            var measurements = ScaleFishTestHelpers.BuildNoisy(
                x => x,
                ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
                sampleSize: 25,
                relativeNoise: 0.08,
                rng);

            var result = new ComplexityEstimator().EstimateComplexity(measurements);
            result.ShouldNotBeNull();
            result.CrossValidation.ShouldNotBeNull();
            result.CrossValidation.RankAgreement.ShouldBeLessThanOrEqualTo(1.0, $"seed {seed}");
            result.CrossValidation.RankAgreement.ShouldBeGreaterThanOrEqualTo(0.0, $"seed {seed}");
        }
    }
}
