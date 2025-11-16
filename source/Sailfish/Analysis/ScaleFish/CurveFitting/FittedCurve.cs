namespace Sailfish.Analysis.ScaleFish.CurveFitting;

public class FittedCurve
{
    public FittedCurve(double scale, double bias)
    {
        Scale = scale;
        Bias = bias;
    }

    public double Scale { get; set; }
    public double Bias { get; set; }
}