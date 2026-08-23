using Sailfish.Analysis.SailDiff.Statistics;

namespace Sailfish.Analysis.SailDiff;

/// <summary>
/// The single source of truth for how a method-vs-method comparison is turned into display text — the
/// significance-to-verdict decision, the verdict vocabulary, and the p/q-value presentation. Every surface
/// (console markdown, CSV, IDE NxN matrix, baseline table) renders through these helpers so the same run can
/// never report a different verdict word or a differently-formatted q-value depending on where you look.
/// </summary>
public static class MethodComparisonDisplay
{
    /// <summary>
    /// Maps a q-value + alpha + effect-size ratio to the cohort verdict. This is the one place the
    /// "significant after BH-FDR, then direction from the ratio" rule lives; <see cref="MethodComparisonAnalyzer" />
    /// and any surface that must orient a mirrored cell itself both call through here.
    /// </summary>
    /// <remarks>ratio = compared / primary, so ratio &lt; 1 means the contender is faster ⇒ Improved.</remarks>
    public static MethodComparisonVerdict Verdict(double qValue, double alpha, double ratio)
    {
        if (!SailDiffSignificance.IsSignificantPositive(qValue, alpha)) return MethodComparisonVerdict.Similar;
        return ratio < 1.0 ? MethodComparisonVerdict.Improved : MethodComparisonVerdict.Slower;
    }

    /// <summary>The display label for a verdict. "Similar" means not significant after FDR — never "No Change".</summary>
    public static string Label(MethodComparisonVerdict verdict) => verdict switch
    {
        MethodComparisonVerdict.Improved => "Improved",
        MethodComparisonVerdict.Slower => "Slower",
        _ => "Similar"
    };

    /// <summary>
    /// The CSV "change description" column vocabulary, derived from the same verdict so it can never diverge
    /// from the label. Regressed ⇔ Slower, Improved ⇔ Improved, No Change ⇔ Similar.
    /// </summary>
    public static string ChangeDescription(MethodComparisonVerdict verdict) => verdict switch
    {
        MethodComparisonVerdict.Improved => "Improved",
        MethodComparisonVerdict.Slower => "Regressed",
        _ => "No Change"
    };

    /// <summary>
    /// The single p/q-value presentation used across every comparison surface: scientific notation below
    /// 1e-3, otherwise three significant decimals.
    /// </summary>
    public static string FormatPValue(double p) => p < 1e-3 ? p.ToString("0.0e-0") : p.ToString("0.###");
}
