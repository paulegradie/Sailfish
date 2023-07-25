using System.Linq;
using System.Threading.Tasks;
using Sailfish.ComplexityEstimation;
using Sailfish.ComplexityEstimation.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Test.ComplexityEstimation;

public class ComplexityEstimatorFixture
{
    private readonly ComplexityEstimator estimator = new();

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Linear()
    {
        Assert<Linear>();
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_NLogN()
    {
        Assert<NLogN>();
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Quadratic()
    {
        Assert<Quadratic>();
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Cubic()
    {
        Assert<Cubic>();
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_LogLinear()
    {
        Assert<LogLinear>();
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Exponential()
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
    public void EstimatorFindsCorrectComplexity_LogLogN()
    {
        Assert<LogLogN>();
    }

    private void Assert<TComplexityFunction>() where TComplexityFunction : ComplexityFunction, new()
    {
        estimator.EstimateComplexity(GetMeasurements<TComplexityFunction>()).ComplexityFunction.Name.ShouldBe(typeof(TComplexityFunction).Name);
    }

    static ComplexityMeasurement[] GetMeasurements<TComplexityFunction>() where TComplexityFunction : ComplexityFunction, new()
    {
        var instance = new TComplexityFunction();
        return Enumerable.Range(1, 100).Select(i => new ComplexityMeasurement(i, instance.Compute(i))).ToArray();
    }
}