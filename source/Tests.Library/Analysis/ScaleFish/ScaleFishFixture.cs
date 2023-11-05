using System;
using System.Linq;
using System.Reflection;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ScaleFishFixture
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
    public void EstimatorFindsCorrectComplexity_LogLinear()
    {
        Assert<LogLinear>();
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


    private void Assert<TComplexityFunction>() where TComplexityFunction : ScaleFishModelFunction
    {
        new ComplexityEstimator().EstimateComplexity(GetMeasurements<TComplexityFunction>()).ScaleFishModelFunction.Name.ShouldBe(typeof(TComplexityFunction).Name);
    }

    private static ComplexityMeasurement[] GetMeasurements<TComplexityFunction>() where TComplexityFunction : ScaleFishModelFunction
    {
        var constructor = typeof(TComplexityFunction)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        var instance = constructor.Invoke(new object[] {  }) as ScaleFishModelFunction;
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