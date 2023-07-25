namespace Sailfish.ComplexityEstimation.ComplexityFunctions;

public class Cubic : ComplexityFunction
{
    public override double Compute(int n)
    {
        return n * n * n;
    }

    public override string Name { get; set; } = nameof(Cubic);
    public override string OName { get; set; } = "O(n^3)";
    public override string Quality { get; set; } = "Very Bad";
}