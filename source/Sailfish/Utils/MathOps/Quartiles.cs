namespace Sailfish.Utils.MathOps;

public class Quartiles
{
    public Quartiles(double[] interQuartiles, double[] outerQuartiles)
    {
        InterQuartiles = interQuartiles;
        OuterQuartiles = outerQuartiles;
    }

    public double[] InterQuartiles { get; set; }
    public double[] OuterQuartiles { get; set; }
}