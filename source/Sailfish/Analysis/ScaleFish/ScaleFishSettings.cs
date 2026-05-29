namespace Sailfish.Analysis.ScaleFish;

/// <summary>
/// Tunables for the ScaleFish complexity estimator. All defaults reproduce the out-of-the-box behaviour:
/// rich diagnostics on (bootstrap + continuous exponent), parallel bootstrap, standard Burnham &amp; Anderson
/// distinguishability threshold of Δ-AICc ≥ 2.
/// </summary>
public class ScaleFishSettings
{
    /// <summary>
    /// When true, the estimator runs a bootstrap diagnostic any time raw replicates are present at every X.
    /// Set false to skip the bootstrap entirely (faster, but loses parameter CIs and selection-agreement).
    /// </summary>
    public bool EnableBootstrap { get; set; } = true;

    /// <summary>
    /// Number of bootstrap iterations when <see cref="EnableBootstrap"/> is true. Higher = smoother CIs at
    /// linear extra cost. 0 effectively disables the bootstrap.
    /// </summary>
    public int BootstrapIterations { get; set; } = 200;

    /// <summary>
    /// When true, bootstrap iterations run across multiple threads with deterministic per-iteration seeds.
    /// </summary>
    public bool EnableParallelBootstrap { get; set; } = true;

    /// <summary>
    /// When true, the estimator fits the continuous power-log model and attaches a (b, c) diagnostic to
    /// the result. Set false to skip this calculation.
    /// </summary>
    public bool EnableContinuousExponent { get; set; } = true;

    /// <summary>
    /// Δ-AICc threshold used to decide whether the best model is statistically separable from the runner-up.
    /// 2.0 is the standard Burnham &amp; Anderson cutoff for "some evidence"; raise it to be more conservative,
    /// lower it to be more permissive.
    /// </summary>
    public double DistinguishabilityDelta { get; set; } = 2.0;

    /// <summary>
    /// When true, the estimator runs leave-one-X-out cross-validation as a hold-out check on the
    /// AICc-based classification. Adds a <see cref="CurveFitting.CrossValidationDiagnostic"/> to the
    /// result. Cheap for typical N counts (5–10 X values).
    /// </summary>
    public bool EnableCrossValidation { get; set; } = true;

    /// <summary>
    /// When true, additional ScaleFish fits are run on per-X percentiles (default p50/p95/p99) of the
    /// raw replicates, surfaced via the model's <c>TailFits</c> dictionary. Useful for detecting tail
    /// behaviour that diverges from the mean — GC pauses, contention scaling, etc.
    /// </summary>
    public bool EnableTailPercentileFits { get; set; } = true;

    /// <summary>
    /// Percentiles to fit when <see cref="EnableTailPercentileFits"/> is true. Values in (0, 1).
    /// Defaults to { 0.50, 0.95, 0.99 }.
    /// </summary>
    public double[] TailPercentiles { get; set; } = new[] { 0.50, 0.95, 0.99 };

    /// <summary>
    /// When true, every ScaleFish run appends a snapshot to a history file in the tracking directory and
    /// the markdown report includes a transition section if a previous snapshot existed for this method.
    /// </summary>
    public bool EnableTrendTracking { get; set; } = true;

    /// <summary>
    /// When true, the estimator writes a standalone HTML report next to the markdown one. Includes the
    /// empirical points, best/runner-up curves, bootstrap CI band, residuals, and tail-percentile minis.
    /// </summary>
    public bool EmitHtmlReport { get; set; } = true;

    public static ScaleFishSettings Default => new();
}
