using Sailfish.Analysis.ComplexityEstimation.ComplexityFunctions;
using Sailfish.Analysis.ComplexityEstimation.CurveFitting;

namespace Sailfish.Analysis.ComplexityEstimation;

public static class ComplexityReferences
{
    public static IComplexityFunction[] GetComplexityFunctions()
    {
        var fitnessCalculator = new FitnessCalculator();
        return new IComplexityFunction[]
        {
            new Linear(fitnessCalculator),
            new NLogN(fitnessCalculator),
            new Quadratic(fitnessCalculator),
            new Cubic(fitnessCalculator),
            new LogLinear(fitnessCalculator),
            new Exponential(fitnessCalculator),
            new Factorial(fitnessCalculator),
            new SqrtN(fitnessCalculator)
            // new LogLogN()
        };
    }
}