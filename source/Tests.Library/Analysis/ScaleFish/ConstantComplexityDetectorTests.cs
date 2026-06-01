using System;
using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Unit tests for <see cref="ConstantComplexityDetector"/> — the runtime backstop that flags a scaleFish
/// variable whose fit looks ~O(1) (the classic "N-dependent state built in [SailfishGlobalSetup] and
/// frozen across all variable sets" trap). The decision is exercised against real
/// <see cref="ComplexityEstimator"/> fits so the test proves end-to-end behaviour, plus a few constructed
/// models to pin the guards.
/// </summary>
public class ConstantComplexityDetectorTests
{
    /// <summary>
    /// Flat timings across the whole variable range — the signature of a frozen variable — must be flagged
    /// as ~O(1). Mirrors the noisy-fit style used elsewhere in the ScaleFish suite.
    /// </summary>
    [Fact]
    public void FlatTimings_AcrossVariableRange_AreFlaggedConstant()
    {
        var rng = new Random(1234);
        // Every X measures essentially the same time (the GlobalSetup-frozen-state case).
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            trueFunction: _ => 500.0,
            xs: ScaleFishTestHelpers.LogSpacedX(4, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.03,
            rng);

        var model = new ComplexityEstimator().EstimateComplexity(measurements);
        model.ShouldNotBeNull();

        ConstantComplexityDetector
            .IsLikelyConstant(model, measurements)
            .ShouldBeTrue("flat timings across the variable range should read as ~O(1)");
    }

    /// <summary>
    /// A clearly-linear benchmark must NOT be flagged — the variable genuinely drives runtime.
    /// </summary>
    [Fact]
    public void LinearScaling_IsNotFlaggedConstant()
    {
        var rng = new Random(1234);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            trueFunction: x => 10.0 * x + 50.0,
            xs: ScaleFishTestHelpers.LogSpacedX(4, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        var model = new ComplexityEstimator().EstimateComplexity(measurements);
        model.ShouldNotBeNull();
        model.ScaleFishModelFunction.Name.ShouldBe(nameof(Linear));

        ConstantComplexityDetector
            .IsLikelyConstant(model, measurements)
            .ShouldBeFalse("a clearly-linear fit must not be mistaken for ~O(1)");
    }

    /// <summary>
    /// A clearly-quadratic benchmark must NOT be flagged either.
    /// </summary>
    [Fact]
    public void QuadraticScaling_IsNotFlaggedConstant()
    {
        var rng = new Random(99);
        var quadratic = new Quadratic();
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            trueFunction: x => quadratic.Compute(0, 1, x),
            xs: ScaleFishTestHelpers.LogSpacedX(4, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        var model = new ComplexityEstimator().EstimateComplexity(measurements);
        model.ShouldNotBeNull();

        ConstantComplexityDetector
            .IsLikelyConstant(model, measurements)
            .ShouldBeFalse("a clearly-quadratic fit must not be mistaken for ~O(1)");
    }

    /// <summary>
    /// The guard: a fit the data can confidently separate into a growth family is never flagged, even if
    /// its measured span over the supplied range happens to be small. Distinguishability wins.
    /// </summary>
    [Fact]
    public void DistinguishableFit_IsNeverFlagged_EvenWithFlatLookingRange()
    {
        var linear = new Linear { FunctionParameters = new FittedCurve(scale: 1.0, bias: 1_000_000.0) };
        var next = new Quadratic { FunctionParameters = new FittedCurve(scale: 1.0, bias: 0.0) };
        // Distinguishable = true ⇒ detector must short-circuit to false regardless of span.
        var model = new ScaleFishModel(
            linear, goodnessOfFit: 0.999,
            next, nextClosestGoodnessOfFit: 0.5,
            bestAicc: 1.0, nextBestAicc: 50.0, akaikeWeight: 0.99,
            isDistinguishable: true, sampleSize: 6, powerLog: null);

        // Even over a range where scale*x is tiny next to the bias, distinguishability must win.
        var measurements = new List<ComplexityMeasurement>
        {
            new(1, 1_000_001), new(2, 1_000_002)
        };

        ConstantComplexityDetector.IsLikelyConstant(model, measurements).ShouldBeFalse();
    }

    /// <summary>
    /// Indistinguishable + near-zero slope over the measured range ⇒ flagged. Pins the relative-span path
    /// with a hand-built model so the threshold behaviour is explicit.
    /// </summary>
    [Fact]
    public void Indistinguishable_NearZeroSlope_IsFlagged()
    {
        // f(x) = 0.0001*x + 500 ⇒ over [4, 256] the curve moves ~0.025 on a level of ~500 ⇒ <5%.
        var linear = new Linear { FunctionParameters = new FittedCurve(scale: 0.0001, bias: 500.0) };
        var next = new NLogN { FunctionParameters = new FittedCurve(scale: 0.0001, bias: 500.0) };
        var model = new ScaleFishModel(
            linear, goodnessOfFit: 0.2,
            next, nextClosestGoodnessOfFit: 0.2,
            bestAicc: 10.0, nextBestAicc: 10.5, akaikeWeight: 0.4,
            isDistinguishable: false, sampleSize: 6, powerLog: null);

        var measurements = new List<ComplexityMeasurement> { new(4, 500), new(256, 500) };

        ConstantComplexityDetector.IsLikelyConstant(model, measurements).ShouldBeTrue();
    }

    /// <summary>
    /// Indistinguishable but with a real slope over the measured range ⇒ NOT flagged. Confirms the span
    /// threshold rejects genuinely-moving curves even when the family is ambiguous (e.g. Linear vs NLogN).
    /// </summary>
    [Fact]
    public void Indistinguishable_WithRealSlope_IsNotFlagged()
    {
        // f(x) = 10*x + 50 ⇒ over [4, 256] the curve moves from 90 to 2610 ⇒ ~97% span.
        var linear = new Linear { FunctionParameters = new FittedCurve(scale: 10.0, bias: 50.0) };
        var next = new NLogN { FunctionParameters = new FittedCurve(scale: 10.0, bias: 50.0) };
        var model = new ScaleFishModel(
            linear, goodnessOfFit: 0.98,
            next, nextClosestGoodnessOfFit: 0.97,
            bestAicc: 10.0, nextBestAicc: 10.5, akaikeWeight: 0.4,
            isDistinguishable: false, sampleSize: 6, powerLog: null);

        var measurements = new List<ComplexityMeasurement> { new(4, 90), new(256, 2610) };

        ConstantComplexityDetector.IsLikelyConstant(model, measurements).ShouldBeFalse();
    }

    /// <summary>
    /// Fallback path: with no measurements available, an indistinguishable fit whose continuous power-log
    /// exponents are ~0 is treated as constant.
    /// </summary>
    [Fact]
    public void NoMeasurements_FlatPowerLog_IsFlagged()
    {
        var linear = new Linear { FunctionParameters = new FittedCurve(scale: 0.0, bias: 500.0) };
        var next = new NLogN { FunctionParameters = new FittedCurve(scale: 0.0, bias: 500.0) };
        var powerLog = new PowerLogResult(a: 1.0, b: 0.01, c: 0.01, d: 500.0, rSquared: 0.1);
        var model = new ScaleFishModel(
            linear, goodnessOfFit: 0.2,
            next, nextClosestGoodnessOfFit: 0.2,
            bestAicc: 10.0, nextBestAicc: 10.4, akaikeWeight: 0.4,
            isDistinguishable: false, sampleSize: 6, powerLog: powerLog);

        ConstantComplexityDetector.IsLikelyConstant(model, measurements: null).ShouldBeTrue();
    }

    /// <summary>
    /// Fallback path: with no measurements and a power-log exponent that indicates real growth (b ≈ 1),
    /// the fit is not flagged.
    /// </summary>
    [Fact]
    public void NoMeasurements_GrowingPowerLog_IsNotFlagged()
    {
        var linear = new Linear { FunctionParameters = new FittedCurve(scale: 10.0, bias: 50.0) };
        var next = new NLogN { FunctionParameters = new FittedCurve(scale: 10.0, bias: 50.0) };
        var powerLog = new PowerLogResult(a: 10.0, b: 1.0, c: 0.0, d: 50.0, rSquared: 0.98);
        var model = new ScaleFishModel(
            linear, goodnessOfFit: 0.98,
            next, nextClosestGoodnessOfFit: 0.97,
            bestAicc: 10.0, nextBestAicc: 10.4, akaikeWeight: 0.4,
            isDistinguishable: false, sampleSize: 6, powerLog: powerLog);

        ConstantComplexityDetector.IsLikelyConstant(model, measurements: null).ShouldBeFalse();
    }

    /// <summary>
    /// With neither measurements nor a power-log diagnostic, the detector is conservative and returns false
    /// rather than guessing — avoids spurious warnings on deserialised/legacy models.
    /// </summary>
    [Fact]
    public void NoMeasurements_NoPowerLog_IsConservativelyFalse()
    {
        var linear = new Linear { FunctionParameters = new FittedCurve(scale: 0.0, bias: 500.0) };
        var next = new NLogN { FunctionParameters = new FittedCurve(scale: 0.0, bias: 500.0) };
        var model = new ScaleFishModel(
            linear, goodnessOfFit: 0.2,
            next, nextClosestGoodnessOfFit: 0.2,
            bestAicc: 10.0, nextBestAicc: 10.4, akaikeWeight: 0.4,
            isDistinguishable: false, sampleSize: 6, powerLog: null);

        ConstantComplexityDetector.IsLikelyConstant(model, measurements: null).ShouldBeFalse();
    }

    [Fact]
    public void NullModel_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ConstantComplexityDetector.IsLikelyConstant(null!, measurements: null));
    }

    /// <summary>
    /// The hint must name the offending variable and point at the GlobalSetup/MethodSetup cause so the user
    /// can act on it. Phrased as a question/hint, never an assertion of error.
    /// </summary>
    [Fact]
    public void BuildWarningMessage_NamesVariable_AndPointsAtSetupCause()
    {
        var fn = new Linear { FunctionParameters = new FittedCurve(scale: 0.0, bias: 500.0) };
        var model = ScaleFishModelBuilderModel(fn);

        var message = ConstantComplexityDetector.BuildWarningMessage("N", model);

        message.ShouldContain("'N'");
        message.ShouldContain("~O(1)");
        message.ShouldContain("[SailfishGlobalSetup]");
        message.ShouldContain("[SailfishMethodSetup]");
    }

    private static ScaleFishModel ScaleFishModelBuilderModel(ScaleFishModelFunction primary)
    {
        return new ScaleFishModel(
            primary, goodnessOfFit: 0.2,
            new Quadratic { FunctionParameters = new FittedCurve(scale: 0.0, bias: 500.0) },
            nextClosestGoodnessOfFit: 0.2,
            bestAicc: 10.0, nextBestAicc: 10.4, akaikeWeight: 0.4,
            isDistinguishable: false, sampleSize: 6, powerLog: null);
    }
}
