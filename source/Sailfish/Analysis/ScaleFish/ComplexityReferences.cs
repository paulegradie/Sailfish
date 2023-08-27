using System.Collections.Generic;
using Sailfish.Analysis.Scalefish.ComplexityFunctions;

namespace Sailfish.Analysis.Scalefish;

public static class ComplexityReferences
{
    public static IEnumerable<ComplexityFunction> GetComplexityFunctions()
    {
        // if you add to this list, be sure to add also to the ComplexityFunctionConverter
        return new ComplexityFunction[]
        {
            new Linear(),
            new NLogN(),
            new Quadratic(),
            new Cubic(),
            new LogLinear(),
            new Exponential(),
            new Factorial(),
            new SqrtN()
            // new LogLogN()
        };
    }
}