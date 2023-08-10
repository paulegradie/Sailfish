using System.Collections.Generic;

namespace Sailfish.Statistics.Tests;

public class TestResults
{
    public TestResults(
        double meanBefore,
        double meanAfter,
        double medianBefore,
        double medianAfter,
        double testStatistic,
        double pValue,
        string changeDescription,
        int sampleSizeBefore,
        int sampleSizeAfter,
        double[] rawDataBefore,
        double[] rawDataAfter,
        Dictionary<string, object> additionalResults)
    {
        MeanBefore = meanBefore;
        MeanAfter = meanAfter;
        MedianBefore = medianBefore;
        MedianAfter = medianAfter;
        TestStatistic = testStatistic;
        PValue = pValue;
        ChangeDescription = changeDescription;
        SampleSizeBefore = sampleSizeBefore;
        SampleSizeAfter = sampleSizeAfter;
        RawDataBefore = rawDataBefore;
        RawDataAfter = rawDataAfter;
        AdditionalResults = additionalResults;
    }

    public double MeanBefore { get; set; }
    public double MeanAfter { get; set; }
    public double MedianBefore { get; }
    public double MedianAfter { get; }
    public double TestStatistic { get; set; }
    public double PValue { get; set; }
    public string ChangeDescription { get; set; }
    public int SampleSizeBefore { get; set; }
    public int SampleSizeAfter { get; set; }
    public double[] RawDataBefore { get; set; }
    public double[] RawDataAfter { get; set; }
    public Dictionary<string, object> AdditionalResults { get; }
}