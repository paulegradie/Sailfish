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
    public TestSettings(double alpha, int round, bool useInnerQuartile = false, TestType testType = TestType.TwoSampleWilcoxonSignedRankTest)
    {
        Alpha = alpha;
        Round = round;
        TestType = testType;
        UseInnerQuartile = useInnerQuartile;
    }

    public double Alpha { get; set; }
    public int Round { get; }
    public TestType TestType { get; }
    public bool UseInnerQuartile { get; set; }
}