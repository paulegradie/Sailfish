namespace Sailfish.Statistics.StatisticalAnalysis;

public class NamedTTestResult
{
    public NamedTTestResult(string testName, TTestResult r)
    {
        TestName = testName;
        MeanOfAfter = r.MeanOfAfter;
        MeanOfBefore = r.MeanOfBefore;
        TStatistic = r.TStatistic;
        PValue = r.PValue;
        DegreesOfFreedom = r.DegreesOfFreedom;
        ChangeDescription = r.ChangeDescription;
    }

    public string TestName { get; set; }
    public double MeanOfBefore { get; set; }
    public double MeanOfAfter { get; set; }
    public double TStatistic { get; set; }
    public double PValue { get; set; }
    public double DegreesOfFreedom { get; set; }
    public string ChangeDescription { get; set; }
}