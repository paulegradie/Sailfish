namespace Sailfish.Analysis.ScaleFish.CurveFitting;

/// <summary>
/// Slim ScaleFish-style result for a single percentile (e.g. p50, p95, p99) of the raw replicates at each X.
/// Carries the family classification and core diagnostics; deliberately omits nested bootstrap/CV/tail
/// fits so the model file doesn't explode combinatorially.
/// </summary>
public class TailFitResult
{
    public TailFitResult(
        double percentile,
        string bestFamilyName,
        string bestFamilyOName,
        double bestRSquared,
        string nextFamilyName,
        double nextRSquared,
        double bestAicc,
        double nextBestAicc,
        double akaikeWeight,
        bool isDistinguishable,
        int sampleSize,
        double bestScale,
        double bestBias)
    {
        Percentile = percentile;
        BestFamilyName = bestFamilyName;
        BestFamilyOName = bestFamilyOName;
        BestRSquared = bestRSquared;
        NextFamilyName = nextFamilyName;
        NextRSquared = nextRSquared;
        BestAicc = bestAicc;
        NextBestAicc = nextBestAicc;
        AkaikeWeight = akaikeWeight;
        IsDistinguishable = isDistinguishable;
        SampleSize = sampleSize;
        BestScale = bestScale;
        BestBias = bestBias;
    }

    /// <summary>Percentile in (0, 1), e.g. 0.95.</summary>
    public double Percentile { get; init; }

    public string BestFamilyName { get; init; }
    public string BestFamilyOName { get; init; }
    public double BestRSquared { get; init; }
    public string NextFamilyName { get; init; }
    public double NextRSquared { get; init; }

    public double BestAicc { get; init; }
    public double NextBestAicc { get; init; }
    public double AkaikeWeight { get; init; }
    public bool IsDistinguishable { get; init; }
    public int SampleSize { get; init; }

    /// <summary>Fitted <c>scale</c> coefficient of the best family at this percentile.</summary>
    public double BestScale { get; init; }

    /// <summary>Fitted <c>bias</c> intercept of the best family at this percentile.</summary>
    public double BestBias { get; init; }

    public double DeltaAicc =>
        double.IsNaN(BestAicc) || double.IsNaN(NextBestAicc)
            ? double.NaN
            : NextBestAicc - BestAicc;
}
