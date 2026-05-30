using System;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Models;

public class StatisticalTestResult
{
#pragma warning disable CS8618

    public StatisticalTestResult(Exception ex)
#pragma warning restore CS8618
    {
        Failed = true;
        Exception = ex;
    }

    public StatisticalTestResult(
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
        Exception = null!;
    }

    public bool Failed { get; set; }
    public Exception Exception { get; set; }

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

    /// <summary>
    /// Standardised effect-size estimate for the comparison — Hedges' g for the t-test,
    /// Cliff's delta for the rank-sum, etc. <c>null</c> when the test doesn't define one
    /// (e.g. KS, where the test statistic itself <em>is</em> the effect size).
    /// </summary>
    public EffectSizeReport? EffectSize { get; set; }

    /// <summary>
    /// Point estimate of the location shift between before and after, with a CI at the
    /// configured significance level. Provides the magnitude answer alongside the binary
    /// significance verdict — "by how much" rather than just "is there a difference?".
    /// <c>null</c> when the test doesn't produce a shift estimate.
    /// </summary>
    public DifferenceReport? Difference { get; set; }

    /// <summary>
    /// Benjamini-Hochberg–adjusted q-value for this comparison within the family of all
    /// pairs in the current SailDiff run. Populated by
    /// <c>StatisticalTestComputer.ComputeTest</c> after every pair has been tested.
    /// <c>null</c> when the test failed or only a single comparison ran.
    /// </summary>
    public double? QValue { get; set; }
}