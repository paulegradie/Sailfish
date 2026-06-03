using System.Globalization;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     Shared, culture-invariant number rendering for the prompt sections, so every figure the model reads is
///     formatted identically (and <c>NaN</c> reads as "n/a" rather than leaking a raw float).
/// </summary>
internal static class SkipperNumberFormat
{
    /// <summary>Six significant figures, invariant culture; <c>NaN</c> renders as "n/a".</summary>
    public static string Num(double value) =>
        double.IsNaN(value) ? "n/a" : value.ToString("G6", CultureInfo.InvariantCulture);

    /// <summary>As <see cref="Num" />, with an explicit leading "+" for non-negative values (for deltas).</summary>
    public static string Signed(double value) => (value >= 0 ? "+" : "") + Num(value);
}
