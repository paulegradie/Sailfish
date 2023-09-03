namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Factorial : ComplexityFunction
{
    public override double Compute(double n, double scale, double bias)
    {
        if (n <= 1)
            return scale * 1 + bias;

        double result = 1;
        for (var i = 2; i <= n; i++)
        {
            result *= i;
        }

        return scale * result + bias;
    }

    public override string Name { get; set; } = nameof(Factorial);
    public override string OName { get; set; } = "O(n!)";
    public override string Quality { get; set; } = "Worst!";
}