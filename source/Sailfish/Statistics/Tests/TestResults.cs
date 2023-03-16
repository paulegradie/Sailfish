using System.Collections.Generic;

namespace Sailfish.Statistics.Tests;

public class TestResults
{
    public TestResults(
        double meanOfBefore,
        double meanOfAfter,
        double medianOfBefore,
        double medianOfAfter,
        double testStatistic,
        double pValue,
        string changeDescription,
        int sampleSizeBefore,
        int sampleSizeAfter,
        Dictionary<string, object> additionalResults)
    {
        MeanOfBefore = meanOfBefore;
        MeanOfAfter = meanOfAfter;
        MedianOfBefore = medianOfBefore;
        MedianOfAfter = medianOfAfter;
        TestStatistic = testStatistic;
        PValue = pValue;
        ChangeDescription = changeDescription;
        SampleSizeBefore = sampleSizeBefore;
        SampleSizeAfter = sampleSizeAfter;
        AdditionalResults = additionalResults;
    }

    public double MeanOfBefore { get; set; }
    public double MeanOfAfter { get; set; }
    public double MedianOfBefore { get; }
    public double MedianOfAfter { get; }
    public double TestStatistic { get; set; }
    public double PValue { get; set; }
    public string ChangeDescription { get; set; }
    public int SampleSizeBefore { get; }
    public int SampleSizeAfter { get; }
    public Dictionary<string, object> AdditionalResults { get; }
}