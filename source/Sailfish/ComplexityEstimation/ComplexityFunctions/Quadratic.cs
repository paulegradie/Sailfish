namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class Quadratic : ComplexityFunction
{
    public override double Compute(int n)
    {
        return n * n;
    }

    public override string Name { get; set; } = "Quadratic";
    public override string OName { get; set; } = "O(n^2)";
    public override string Quality { get; set; } = "Bad";
}