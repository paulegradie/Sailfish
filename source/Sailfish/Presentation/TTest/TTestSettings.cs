namespace Sailfish.Presentation.TTest;

public class TTestSettings
{
    public TTestSettings(double alpha, int round)
    {
        Alpha = alpha;
        Round = round;
    }
    public double Alpha { get; set; } = 0.01;
    public int Round { get; }
}