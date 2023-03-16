namespace Sailfish.Analysis;

public class TestSettings
{
    /// <summary>
    /// Settings to use with the regression tester.
    /// 
    /// </summary>
    /// <param name="alpha">alpha is the significance threshold. Fewer iterations need a lower alpha for good discrimination between before and after. Typical may be 0.01 or lower.</param>
    /// <param name="round">The number of decimal places to round to. Typical is 4.</param>
    /// <param name="testType"></param>
    /// <param name="useInnerQuartile"></param>
    public TestSettings(double alpha = 0.001, int round = 3, bool useInnerQuartile = false, TestType testType = TestType.KolmogorovSmirnovTest)
    {
        Alpha = alpha;
        Round = round;
        UseInnerQuartile = useInnerQuartile;
        TestType = testType;
    }

    public double Alpha { get; set; }
    public int Round { get; }
    public bool UseInnerQuartile { get; set; }
    public TestType TestType { get; }
}