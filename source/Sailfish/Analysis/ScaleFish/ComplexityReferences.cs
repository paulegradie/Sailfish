using Sailfish.Analysis.Scalefish.ComplexityFunctions;
using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish;

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