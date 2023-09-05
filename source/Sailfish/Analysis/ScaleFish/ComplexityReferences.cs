using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;

namespace Sailfish.Analysis.ScaleFish;

public static class ComplexityReferences
{
    public static IEnumerable<ScaleFishModelFunction> GetComplexityFunctions()
    {
        // if you add to this list, be sure to add also to the ComplexityFunctionConverter
        return new ScaleFishModelFunction[]
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