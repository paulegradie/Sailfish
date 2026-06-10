namespace Sailfish.Contracts.Public.Models;

/// <summary>
/// Result of a TOST (two one-sided tests) equivalence check on log-time. Answers the question a
/// plain significance test cannot: "are these two samples <em>demonstrably similar</em>, within a
/// margin I consider performance-equivalent?" — separating "no regression larger than X%" from
/// "the run was too noisy to tell".
/// </summary>
/// <param name="MarginPercent">
/// The user-configured equivalence margin as a percentage. The equivalence band on the ratio
/// scale is [1/(1+m/100), 1+m/100] — symmetric in log space.
/// </param>
/// <param name="PValueLower">
/// One-sided p-value against H0: ratio ≥ 1 + margin (the "not slower than the margin" test).
/// </param>
/// <param name="PValueUpper">
/// One-sided p-value against H0: ratio ≤ 1/(1 + margin) (the "not faster than the margin" test).
/// </param>
/// <param name="PValue">
/// The TOST p-value — the larger of the two one-sided p-values. Equivalence is declared when this
/// is ≤ the configured alpha.
/// </param>
/// <param name="IsEquivalent">
/// True when both one-sided tests reject at the configured alpha — the data demonstrate the true
/// ratio lies inside the margin. False means equivalence was <em>not established</em>: either a
/// real difference exists, or the run lacked the power to tell (check
/// <see cref="StatisticalTestResult.MinimumDetectableEffectPercent"/> to distinguish the two).
/// </param>
public sealed record EquivalenceTestResult(
    double MarginPercent,
    double PValueLower,
    double PValueUpper,
    double PValue,
    bool IsEquivalent);
