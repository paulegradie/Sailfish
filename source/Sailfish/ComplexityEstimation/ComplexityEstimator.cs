using System.Linq;
using MathNet.Numerics.Statistics;

namespace Sailfish.ComplexityEstimation;

public class ComplexityEstimator
{
    public ComplexityResult EstimateComplexity(ComplexityMeasurement[] measurements)
    {
        var complexityFunctions = ComplexityReferences.GetComplexityFunctions();

        var errors = complexityFunctions.Select(x => x.ComputeError(measurements)).ToList();
        var minError = errors.Min();
        var closestComplexity = complexityFunctions[errors.ToList().IndexOf(minError)];

        return new ComplexityResult(closestComplexity, minError, errors.Mean(), errors.Median());
    }
}

public record ComplexityResult(IComplexityFunction ComplexityFunction, double Error, double MeanError, double MedianError);