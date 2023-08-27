using System;
using System.Linq;
using System.Reflection;
using Sailfish.Analysis.Scalefish;
using Sailfish.Analysis.Scalefish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Test.ComplexityEstimation;

public class ComplexityEstimatorFixture
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


    private void Assert<TComplexityFunction>() where TComplexityFunction : ComplexityFunction
    {
        new ComplexityEstimator().EstimateComplexity(GetMeasurements<TComplexityFunction>()).ComplexityFunction.Name.ShouldBe(typeof(TComplexityFunction).Name);
    }

    static ComplexityMeasurement[] GetMeasurements<TComplexityFunction>() where TComplexityFunction : ComplexityFunction
    {
        var constructor = typeof(TComplexityFunction)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        var instance = constructor.Invoke(new object[] {  }) as ComplexityFunction;
        instance.ShouldNotBeNull();

        const double scale = 1;
        const double bias = 0;
        var measurements = Enumerable.Range(2, 11)
            .Select(Convert.ToDouble)
            .Select(x => x * 3)
            .Select(i => new ComplexityMeasurement(i, instance.Compute(i, scale, bias)))
            .ToArray();
        return measurements;
    }
}