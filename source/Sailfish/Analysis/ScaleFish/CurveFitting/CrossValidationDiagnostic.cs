namespace Sailfish.Analysis.ScaleFish.CurveFitting;

/// <summary>
/// Leave-one-X-out cross-validation diagnostic. For each candidate family, each fold holds out one X value,
/// refits on the remaining points, then scores the predictive error at the held-out point. Aggregating
/// across folds gives both an out-of-sample fit quality (<see cref="MeanPredictionError"/>) and a stability
/// signal (<see cref="RankAgreement"/>): the fraction of folds whose top-ranked family matched the
/// all-data point estimate.
///
/// Complements AICc (which is in-sample) with a genuine hold-out check — particularly valuable at the
/// small N counts ScaleFish typically runs at.
/// </summary>
public class CrossValidationDiagnostic
{
    public CrossValidationDiagnostic(int foldCount, double rankAgreement, double meanPredictionError, double medianPredictionError)
    {
        FoldCount = foldCount;
        RankAgreement = rankAgreement;
        MeanPredictionError = meanPredictionError;
        MedianPredictionError = medianPredictionError;
    }

    /// <summary>Number of leave-one-out folds executed. Equals the number of usable X values.</summary>
    public int FoldCount { get; init; }

    /// <summary>
    /// Fraction of folds that selected the same best family as the all-data point estimate.
    /// 1.0 ⇒ perfectly stable across hold-outs; values near 1/k (k = candidate count) ⇒ effectively random.
    /// </summary>
    public double RankAgreement { get; init; }

    /// <summary>
    /// Mean absolute prediction error of the all-data winning family across hold-outs, in y-units.
    /// Smaller is better; pair with <see cref="MedianPredictionError"/> to detect outlier folds.
    /// </summary>
    public double MeanPredictionError { get; init; }

    /// <summary>Median absolute prediction error — robust complement to the mean.</summary>
    public double MedianPredictionError { get; init; }
}
