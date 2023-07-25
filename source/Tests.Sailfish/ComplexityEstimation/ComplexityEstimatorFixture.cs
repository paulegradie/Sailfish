using System.Linq;
using System.Threading.Tasks;
using Sailfish.ComplexityEstimation;
using Sailfish.ComplexityEstimation.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Test.ComplexityEstimation;

public class ComplexityEstimatorFixture
{
    readonly ComplexityEstimator estimator = new();

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Linear()
    {
        estimator
            .EstimateComplexity(GetMeasurements<Linear>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(Linear));
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Quadratic()
    {
        estimator
            .EstimateComplexity(GetMeasurements<Quadratic>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(Quadratic));
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Cubic()
    {
        estimator
            .EstimateComplexity(GetMeasurements<Cubic>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(Cubic));
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_LogLinear()
    {
        estimator
            .EstimateComplexity(GetMeasurements<LogLinear>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(LogLinear));
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Exponential()
    {
        estimator
            .EstimateComplexity(GetMeasurements<Exponential>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(Exponential));
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_Factorial()
    {
        estimator
            .EstimateComplexity(GetMeasurements<Factorial>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(Factorial));
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_SqrtN()
    {
        estimator
            .EstimateComplexity(GetMeasurements<SqrtN>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(SqrtN));
    }

    [Fact]
    public async Task EstimatorFindsCorrectComplexity_LogLogN()
    {
        estimator
            .EstimateComplexity(GetMeasurements<LogLogN>())
            .ComplexityFunction.Name
            .ShouldBe(nameof(LogLogN));
    }


    ComplexityMeasurement[] GetMeasurements<TComplexityFunction>() where TComplexityFunction : ComplexityFunction, new()
    {
        var instance = new TComplexityFunction();
        return Enumerable.Range(1, 100).Select(i => new ComplexityMeasurement(i, instance.Compute(i))).ToArray();
    }
}