using System;

namespace Sailfish.Analysis.ScaleFish.Trends;

/// <summary>
/// A single snapshot of a ScaleFish classification, persisted per (test class, method, property) and
/// indexed by commit SHA + timestamp so trend tracking can diff across runs.
/// </summary>
public class ComplexityHistoryEntry
{
    public ComplexityHistoryEntry(
        string testClassFullName,
        string methodName,
        string propertyName,
        string commitSha,
        DateTime timestampUtc,
        string bestFamilyName,
        string bestFamilyOName,
        double bestScale,
        double bestBias,
        double bestRSquared,
        double bestAicc,
        double akaikeWeight,
        bool isDistinguishable,
        int sampleSize,
        double? continuousExponentB,
        double? continuousExponentC,
        double? cvRankAgreement,
        double? bootstrapSelectionAgreement)
    {
        TestClassFullName = testClassFullName;
        MethodName = methodName;
        PropertyName = propertyName;
        CommitSha = commitSha;
        TimestampUtc = timestampUtc;
        BestFamilyName = bestFamilyName;
        BestFamilyOName = bestFamilyOName;
        BestScale = bestScale;
        BestBias = bestBias;
        BestRSquared = bestRSquared;
        BestAicc = bestAicc;
        AkaikeWeight = akaikeWeight;
        IsDistinguishable = isDistinguishable;
        SampleSize = sampleSize;
        ContinuousExponentB = continuousExponentB;
        ContinuousExponentC = continuousExponentC;
        CvRankAgreement = cvRankAgreement;
        BootstrapSelectionAgreement = bootstrapSelectionAgreement;
    }

    /// <summary>Default constructor for JSON deserialisation.</summary>
    public ComplexityHistoryEntry()
    {
        TestClassFullName = string.Empty;
        MethodName = string.Empty;
        PropertyName = string.Empty;
        CommitSha = string.Empty;
        BestFamilyName = string.Empty;
        BestFamilyOName = string.Empty;
    }

    public string TestClassFullName { get; set; }
    public string MethodName { get; set; }
    public string PropertyName { get; set; }
    public string CommitSha { get; set; }
    public DateTime TimestampUtc { get; set; }

    public string BestFamilyName { get; set; }
    public string BestFamilyOName { get; set; }
    public double BestScale { get; set; }
    public double BestBias { get; set; }
    public double BestRSquared { get; set; }
    public double BestAicc { get; set; }
    public double AkaikeWeight { get; set; }
    public bool IsDistinguishable { get; set; }
    public int SampleSize { get; set; }

    public double? ContinuousExponentB { get; set; }
    public double? ContinuousExponentC { get; set; }
    public double? CvRankAgreement { get; set; }
    public double? BootstrapSelectionAgreement { get; set; }

    /// <summary>Stable identity used to match entries across runs: class.method.property.</summary>
    public string Key => $"{TestClassFullName}.{MethodName}.{PropertyName}";
}
