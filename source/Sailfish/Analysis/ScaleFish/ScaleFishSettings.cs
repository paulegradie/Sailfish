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

    public static ScaleFishSettings Default => new();
}
