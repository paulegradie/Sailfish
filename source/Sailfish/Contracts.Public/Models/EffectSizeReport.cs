namespace Sailfish.Contracts.Public.Models;

/// <summary>
/// Standardised or interpretable effect-size estimate for a SailDiff comparison.
/// </summary>
/// <param name="Name">
/// Human-readable identifier — e.g. <c>"Hedges' g"</c> for the t-test path,
/// <c>"Cliff's delta"</c> for the rank-sum path. Lets formatters label the value without
/// hard-coding the test type.
/// </param>
/// <param name="Value">The point estimate. Scale depends on <paramref name="Name"/>.</param>
/// <param name="CiLower">
/// Lower bound of the confidence interval at the configured <c>SailDiffSettings.Alpha</c>,
/// or <c>null</c> when a CI is not available (e.g. degenerate variance, exact CI undefined).
/// </param>
/// <param name="CiUpper">Upper bound of the CI, or <c>null</c> when unavailable.</param>
public sealed record EffectSizeReport(
    string Name,
    double Value,
    double? CiLower,
    double? CiUpper);
