using Sailfish.ComplexityEstimation.ComplexityFunctions;

namespace Sailfish.ComplexityEstimation;

public static class ComplexityReferences
{
    public static IComplexityFunction[] GetComplexityFunctions()
    {
        return new IComplexityFunction[]
        {
            new Linear(),
            new NLogN(),
            new Quadratic(),
            new Cubic(),
            new LogLinear(),
            new Exponential(),
            new Factorial(),
            new SqrtN(),
            new LogLogN()
        };
    }
}