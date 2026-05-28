namespace Sailfish.Analysis.ScaleFish.CurveFitting;

/// <summary>
/// Bootstrap-resampled uncertainty for the best-fit model. Produced by resampling each X's raw replicates
/// with replacement and re-fitting, so the diagnostic is data-driven rather than model-theoretic.
/// </summary>
public class BootstrapDiagnostic
{
    public BootstrapDiagnostic(
        int iterations,
        double selectionAgreement,
        double scaleCiLower,
        double scaleCiUpper,
        double biasCiLower,
        double biasCiUpper)
    {
        Iterations = iterations;
        SelectionAgreement = selectionAgreement;
        ScaleCiLower = scaleCiLower;
        ScaleCiUpper = scaleCiUpper;
        BiasCiLower = biasCiLower;
        BiasCiUpper = biasCiUpper;
    }

    /// <summary>Number of bootstrap iterations run.</summary>
    public int Iterations { get; init; }

    /// <summary>
    /// Fraction of bootstrap iterations that selected the same best-family as the point estimate.
    /// 1.0 ⇒ rock-solid classification; values closer to 1/8 ⇒ effectively random.
    /// </summary>
    public double SelectionAgreement { get; init; }

    /// <summary>Lower 2.5% percentile of the bootstrapped scale parameter.</summary>
    public double ScaleCiLower { get; init; }

    /// <summary>Upper 97.5% percentile of the bootstrapped scale parameter.</summary>
    public double ScaleCiUpper { get; init; }

    /// <summary>Lower 2.5% percentile of the bootstrapped bias parameter.</summary>
    public double BiasCiLower { get; init; }

    /// <summary>Upper 97.5% percentile of the bootstrapped bias parameter.</summary>
    public double BiasCiUpper { get; init; }
}
