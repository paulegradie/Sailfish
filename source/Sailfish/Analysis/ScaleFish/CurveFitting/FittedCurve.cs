namespace Sailfish.Analysis.ScaleFish.CurveFitting;

public class FittedCurve(double scale, double bias)
{
    public double Scale { get; set; } = scale;
    public double Bias { get; set; } = bias;
}