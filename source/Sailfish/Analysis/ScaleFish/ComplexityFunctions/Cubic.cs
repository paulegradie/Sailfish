using System;
using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish.ComplexityFunctions;

public class Cubic : ComplexityFunction
{
    public Cubic(IFitnessCalculator fitnessCalculator) : base(fitnessCalculator)
    {
    }

    public override double Compute(double n, double scale, double bias)
    {
        return scale * Math.Pow(n, 3) + bias;
    }

    public override string Name { get; set; } = nameof(Cubic);
    public override string OName { get; set; } = "O(n^3)";
    public override string Quality { get; set; } = "Very Bad";
}