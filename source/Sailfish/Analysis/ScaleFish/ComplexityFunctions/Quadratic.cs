using System;
using Sailfish.Analysis.Scalefish.CurveFitting;

namespace Sailfish.Analysis.Scalefish.ComplexityFunctions;

public class Quadratic : ComplexityFunction
{
    public Quadratic(IFitnessCalculator fitnessCalculator) : base(fitnessCalculator)
    {
    }

    public override double Compute(double n, double scale, double bias)
    {
        return scale * Math.Pow(n, 2) + bias;
    }

    public override string Name { get; set; } = nameof(Quadratic);
    public override string OName { get; set; } = "O(n^2)";
    public override string Quality { get; set; } = "Bad";
}