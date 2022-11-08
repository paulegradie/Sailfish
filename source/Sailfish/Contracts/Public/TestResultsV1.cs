using System;

namespace Sailfish.Contracts.Public;

#pragma warning disable CS8618

[Obsolete("The properties on this object do not include the most recent set of test result properties. Please considering transitioning to TestResults.")]
public class TestResultsV1
{
    public TestResultsV1(
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