namespace Sailfish.Statistics.StatisticalAnalysis;

public class TTestResult
{
    public TTestResult(
        double meanOfBefore,
        double meanOfAfter,
        double tStatistic,
        double pValue,
        double degreesOfFreedom,
        string changeDescription)
    {
        MeanOfBefore = meanOfBefore;
        MeanOfAfter = meanOfAfter;
        TStatistic = tStatistic;
        PValue = pValue;
        DegreesOfFreedom = degreesOfFreedom;
        ChangeDescription = changeDescription;
    }


    public double MeanOfBefore { get; set; }
    public double MeanOfAfter { get; set; }
    public double TStatistic { get; set; }
    public double PValue { get; set; }
    public double DegreesOfFreedom { get; set; }
    public string ChangeDescription { get; set; }
}