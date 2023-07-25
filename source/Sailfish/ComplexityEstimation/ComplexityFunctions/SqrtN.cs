using System;

namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class SqrtN : ComplexityFunction
{
    public override double Compute(int n)
    {
        return Math.Sqrt(n);
    }

    public override string Name { get; set; } = nameof(SqrtN);
    public override string OName { get; set; } = "O(sqrt(n))";
    public override string Quality { get; set; } = "Okay";
}