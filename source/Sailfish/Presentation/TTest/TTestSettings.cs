namespace Sailfish.Presentation.TTest;

public class TTestSettings
{
    /// <summary>
    /// Settings to use with the T-Tester.
    /// 
    /// </summary>
    /// <param name="alpha">alpha is the significance threshold. Fewer iterations need a lower alpha for good discrimination between before and after. Typical may be 0.01 or lower.</param>
    /// <param name="round">The number of decimal places to round to. Typical is 4.</param>
    public TTestSettings(double alpha, int round)
    {
        Alpha = alpha;
        Round = round;
    }

    public double Alpha { get; set; }
    public int Round { get; }
}