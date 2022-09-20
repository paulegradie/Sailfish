using System.Text.Json.Serialization;
using Sailfish.Statistics.StatisticalAnalysis;

#pragma warning disable CS8618

namespace Sailfish.Contracts.Public;

public class NamedTTestResult
{
    [JsonConstructor]
    public NamedTTestResult()
    {
    }

    public NamedTTestResult(string displayName, TTestResult r)
    {
        DisplayName = displayName;
        MeanOfAfter = r.MeanOfAfter;
        MeanOfBefore = r.MeanOfBefore;
        TStatistic = r.TStatistic;
        PValue = r.PValue;
        DegreesOfFreedom = r.DegreesOfFreedom;
        ChangeDescription = r.ChangeDescription;
    }

    public string DisplayName { get; set; }
    public double MeanOfBefore { get; set; }
    public double MeanOfAfter { get; set; }
    public double TStatistic { get; set; }
    public double PValue { get; set; }
    public double DegreesOfFreedom { get; set; }
    public string ChangeDescription { get; set; }
}