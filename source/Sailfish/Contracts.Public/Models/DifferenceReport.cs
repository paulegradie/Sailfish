namespace Sailfish.Contracts.Public.Models;

/// <summary>
/// Estimate of the location shift between two samples, with a confidence interval at the
/// configured significance level.
/// </summary>
/// <param name="Name">
/// Human-readable identifier — e.g. <c>"Mean difference"</c> for the t-test path,
/// <c>"Hodges-Lehmann shift"</c> for the rank-sum path. Lets formatters describe what
/// the value actually estimates.
/// </param>
/// <param name="Value">Point estimate of the shift. Same units as <paramref name="Units"/>.</param>
/// <param name="CiLower">
/// Lower bound of the CI at the configured <c>SailDiffSettings.Alpha</c>. <c>null</c> when
/// the CI is not computable (e.g. degenerate samples or unsupported by the test).
/// </param>
/// <param name="CiUpper">Upper bound of the CI, or <c>null</c> when unavailable.</param>
/// <param name="Units">
/// Units of the value. Typically <c>"ms"</c>. For log-transformed paths the natural shift
/// is a log-ratio; formatters can render it as <c>"× ratio"</c> after exponentiation.
/// </param>
public sealed record DifferenceReport(
    string Name,
    double Value,
    double? CiLower,
    double? CiUpper,
    string Units);
