using System;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public;

#pragma warning disable CS8618
[Obsolete("Please use TestCaseResults instead of this type")]
public class TestCaseResultsV1
{
    [JsonConstructor]
    public TestCaseResultsV1()
    {
    }

    public TestCaseResultsV1(string displayName, TestResultsV1 r)
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