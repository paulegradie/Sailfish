using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Analysis.ScaleFish;

public class ScaleFishModel
{
    public ScaleFishModel(ScaleFishModelFunction scaleFishModelFunction,
        double goodnessOfFit,
        ScaleFishModelFunction nextClosestScaleFishModelFunction,
        double nextClosestGoodnessOfFit)
        : this(scaleFishModelFunction, goodnessOfFit, nextClosestScaleFishModelFunction, nextClosestGoodnessOfFit,
            bestAicc: double.NaN,
            nextBestAicc: double.NaN,
            akaikeWeight: double.NaN,
            isDistinguishable: false,
            sampleSize: 0,
            powerLog: null)
    {
    }

    public ScaleFishModel(
        ScaleFishModelFunction scaleFishModelFunction,
        double goodnessOfFit,
        ScaleFishModelFunction nextClosestScaleFishModelFunction,
        double nextClosestGoodnessOfFit,
        double bestAicc,
        double nextBestAicc,
        double akaikeWeight,
        bool isDistinguishable,
        int sampleSize,
        PowerLogResult? powerLog)
    {
        ScaleFishModelFunction = scaleFishModelFunction;
        GoodnessOfFit = goodnessOfFit;
        NextClosestScaleFishModelFunction = nextClosestScaleFishModelFunction;
        NextClosestGoodnessOfFit = nextClosestGoodnessOfFit;
        BestAicc = bestAicc;
        NextBestAicc = nextBestAicc;
        AkaikeWeight = akaikeWeight;
        IsDistinguishable = isDistinguishable;
        SampleSize = sampleSize;
        PowerLog = powerLog;
    }

    public ScaleFishModelFunction ScaleFishModelFunction { get; init; }
    public double GoodnessOfFit { get; init; }
    public ScaleFishModelFunction NextClosestScaleFishModelFunction { get; init; }
    public double NextClosestGoodnessOfFit { get; init; }

    /// <summary>
    /// Corrected Akaike Information Criterion (AICc) of the best-fitting family. Lower is better.
    /// NaN when not computed (e.g. older deserialised models).
    /// </summary>
    public double BestAicc { get; init; }

    /// <summary>
    /// AICc of the next-best family. Used together with <see cref="BestAicc"/> to compute Δ-AICc and the
    /// Akaike weight of the best model relative to the runner-up.
    /// </summary>
    public double NextBestAicc { get; init; }

    /// <summary>
    /// Akaike weight of the best model among all candidates: how much relative support the data gives the
    /// best model over the rest. Between 0 and 1. NaN when AICc is unavailable.
    /// </summary>
    public double AkaikeWeight { get; init; }

    /// <summary>
    /// True when the best model is statistically separable from the runner-up at the conventional
    /// Δ-AICc ≥ 2 threshold (i.e. the data really do prefer the best family).
    /// </summary>
    public bool IsDistinguishable { get; init; }

    /// <summary>
    /// The number of (X, Y) measurements used to fit the model. 0 when not recorded.
    /// </summary>
    public int SampleSize { get; init; }

    /// <summary>
    /// Continuous power-log diagnostic: fits y = a · x^b · (log x)^c + d. Null when the diagnostic
    /// is not computable (e.g. too few X &gt; 1 points). Provides a continuous exponent independent of
    /// the discrete-family classification.
    /// </summary>
    public PowerLogResult? PowerLog { get; init; }

    /// <summary>
    /// Bootstrap-resampled uncertainty for the best-fit parameters. Populated only when raw replicates
    /// are available at every X (i.e. real benchmark runs, not synthetic test data).
    /// </summary>
    public BootstrapDiagnostic? Bootstrap { get; init; }

    /// <summary>
    /// Δ-AICc between the next-best and best model. Larger ⇒ more confidently distinguishable.
    /// </summary>
    public double DeltaAicc =>
        double.IsNaN(BestAicc) || double.IsNaN(NextBestAicc)
            ? double.NaN
            : NextBestAicc - BestAicc;
}
