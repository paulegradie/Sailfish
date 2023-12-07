namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Quadratic : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(Quadratic);
    public override string OName { get; set; } = "O(n^2)";
    public override string Quality { get; set; } = "Bad";
    public override string FunctionDef { get; set; } = "f(x) = {0}x^2 + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        return scale * (x * x) + bias;
    }
}