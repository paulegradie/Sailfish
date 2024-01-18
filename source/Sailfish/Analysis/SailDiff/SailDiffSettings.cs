namespace Sailfish.Analysis.SailDiff;

/// <summary>
///     Settings to use with the regression tester.
/// </summary>
/// <param name="alpha">
///     alpha is the significance threshold. Fewer iterations need a lower alpha for good discrimination
///     between before and after. Typical may be 0.01 or lower.
/// </param>
/// <param name="round">The number of decimal places to round to. Typical is 4.</param>
/// <param name="testType"></param>
/// <param name="useOutlierDetection"></param>
/// <param name="maxDegreeOfParallelism"></param>
/// <param name="disableOrdering"></param>
public class SailDiffSettings(
    double alpha = 0.001,
    int round = 3,
    bool useOutlierDetection = true,
    TestType testType = TestType.TwoSampleWilcoxonSignedRankTest,
    int maxDegreeOfParallelism = 4,
    bool disableOrdering = false)
{
    public double Alpha { get; private set; } = alpha;
    public int Round { get; private set; } = round;
    public bool UseOutlierDetection { get; private set; } = useOutlierDetection;
    public TestType TestType { get; private set; } = testType;
    public int MaxDegreeOfParallelism { get; private set; } = maxDegreeOfParallelism;
    public bool DisableOrdering { get; private set; } = disableOrdering;

    public void SetAlpha(double alpha)
    {
        Alpha = alpha;
    }

    public void SetRound(int round)
    {
        Round = round;
    }

    public void DisableOutlierDetection()
    {
        UseOutlierDetection = false;
    }

    public void SetTestType(TestType testType)
    {
        TestType = testType;
    }

    public void SetMaxDegreeOfParallelism(int maxDegreeOfParallelism)
    {
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public void SetDisableOrdering(bool disable)
    {
        DisableOrdering = disable;
    }
}