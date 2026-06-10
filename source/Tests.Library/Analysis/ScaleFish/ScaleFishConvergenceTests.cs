using System;
using System.Linq;
using System.Reflection;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ScaleFishConvergenceTests
{
    [Fact]
    public void EstimatorFindsCorrectComplexity_Linear()
    {
        Assert<Linear>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_NLogN()
    {
        Assert<NLogN>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_Quadratic()
    {
        Assert<Quadratic>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_Cubic()
    {
        Assert<Cubic>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_LogLinearData_ClassifiesAsNLogN()
    {
        // LogLinear's x·log₂(x) basis is a constant multiple of NLogN's x·ln(x); the constant is
        // absorbed into the fitted scale, so the canonical NLogN family is the correct (and only)
        // n·log n candidate now that the collinear clone is deserialization-only.
        var estimation = new ComplexityEstimator().EstimateComplexity(GetMeasurements<LogLinear>());
        estimation.ShouldNotBeNull();
        estimation.ScaleFishModelFunction.Name.ShouldBe(nameof(NLogN));
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_Exponential()
    {
        Assert<Exponential>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_Factorial()
    {
        Assert<Factorial>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_SqrtN()
    {
        Assert<SqrtN>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_Logarithmic()
    {
        Assert<Logarithmic>();
    }

    [Fact]
    public void EstimatorFindsCorrectComplexity_Constant()
    {
        // Flat data: every two-parameter family can match Constant's residuals by fitting
        // scale ≈ 0, but pays the AICc penalty for the spare parameter — the one-parameter
        // Constant family must win.
        var measurements = Enumerable.Range(2, 11)
            .Select(i => new ComplexityMeasurement(i * 3, 42.0))
            .ToArray();

        var estimation = new ComplexityEstimator().EstimateComplexity(measurements);
        estimation.ShouldNotBeNull();
        estimation.ScaleFishModelFunction.Name.ShouldBe(nameof(Constant));
        estimation.ScaleFishModelFunction.FunctionParameters!.Bias.ShouldBe(42.0, tolerance: 1e-9);
    }

    private void Assert<TComplexityFunction>() where TComplexityFunction : ScaleFishModelFunction
    {
        var estimation = new ComplexityEstimator().EstimateComplexity(GetMeasurements<TComplexityFunction>());
        estimation.ShouldNotBeNull();
        estimation.ScaleFishModelFunction.Name.ShouldBe(typeof(TComplexityFunction).Name);
    }

    private static ComplexityMeasurement[] GetMeasurements<TComplexityFunction>() where TComplexityFunction : ScaleFishModelFunction
    {
        var constructor = typeof(TComplexityFunction)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        var instance = constructor.Invoke([]) as ScaleFishModelFunction;
        instance.ShouldNotBeNull();

        const double scale = 1;
        const double bias = 0;
        var measurements = Enumerable.Range(2, 11)
            .Select(Convert.ToDouble)
            .Select(x => x * 3)
            .Select(i => new ComplexityMeasurement(i, instance.Compute(bias, scale, i)))
            .ToArray();
        return measurements;
    }
}