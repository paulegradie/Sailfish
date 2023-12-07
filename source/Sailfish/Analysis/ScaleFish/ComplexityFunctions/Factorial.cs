namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Factorial : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(Factorial);

    public override string OName { get; set; } = "O(n!)";

    public override string Quality { get; set; } = "Worst!";

    public override string FunctionDef { get; set; } = "f(x) = {0}x! + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        if (x <= 1)
            return scale * 1 + bias;

        double result = 1;
        for (var i = 2; i <= x; i++) result *= i;

        return scale * result + bias;
    }
}