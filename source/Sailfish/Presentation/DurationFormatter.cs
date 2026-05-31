using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sailfish.Presentation;

/// <summary>
/// Time units used when rendering durations for humans.
/// </summary>
public enum DurationUnit
{
    Nanoseconds,
    Microseconds,
    Milliseconds,
    Seconds
}

/// <summary>
/// Magnitude-aware formatting for durations. Sailfish stores every duration in milliseconds
/// (see <c>PerformanceRunResult.ConvertFromPerfTimer</c>), so all inputs here are milliseconds.
/// A representative magnitude is used to pick a single, human-friendly unit (ns / µs / ms / s)
/// for a whole table column so that values land at roughly one-to-four significant figures,
/// BenchmarkDotNet-style. This is presentation only — it never changes stored or persisted values.
/// </summary>
public static class DurationFormatter
{
    private const double NsPerMs = 1_000_000.0;
    private const double UsPerMs = 1_000.0;
    private const double SecondsPerMs = 0.001;

    /// <summary>
    /// Selects a single unit for a column/table given the millisecond values it will render.
    /// The unit is chosen from the largest absolute value so the biggest entry lands in [1, 1000).
    /// Empty input or all-zero values fall back to milliseconds (the historical default).
    /// </summary>
    public static DurationUnit SelectUnit(IEnumerable<double> millisecondValues)
    {
        if (millisecondValues is null) return DurationUnit.Milliseconds;

        var maxMagnitude = 0.0;
        foreach (var value in millisecondValues)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) continue;
            var magnitude = Math.Abs(value);
            if (magnitude > maxMagnitude) maxMagnitude = magnitude;
        }

        return PickUnit(maxMagnitude);
    }

    /// <summary>
    /// Selects a unit for a single millisecond value.
    /// </summary>
    public static DurationUnit SelectUnit(double milliseconds) => PickUnit(Math.Abs(milliseconds));

    /// <summary>
    /// Short label for a unit (e.g. "µs").
    /// </summary>
    public static string UnitLabel(DurationUnit unit) => unit switch
    {
        DurationUnit.Nanoseconds => "ns",
        DurationUnit.Microseconds => "µs",
        DurationUnit.Milliseconds => "ms",
        DurationUnit.Seconds => "s",
        _ => "ms"
    };

    /// <summary>
    /// Converts a millisecond value into the supplied unit.
    /// </summary>
    public static double ToUnit(double milliseconds, DurationUnit unit) => unit switch
    {
        DurationUnit.Nanoseconds => milliseconds * NsPerMs,
        DurationUnit.Microseconds => milliseconds * UsPerMs,
        DurationUnit.Milliseconds => milliseconds,
        DurationUnit.Seconds => milliseconds * SecondsPerMs,
        _ => milliseconds
    };

    /// <summary>
    /// Formats a millisecond value in the supplied unit with a fixed number of decimals
    /// (no unit suffix — intended for columns whose unit lives in the header).
    /// </summary>
    public static string Format(double milliseconds, DurationUnit unit, int decimals)
    {
        var value = ToUnit(milliseconds, unit);
        return value.ToString("F" + Math.Max(0, decimals), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a millisecond value in the supplied unit, including the unit suffix (e.g. "1.100 µs").
    /// </summary>
    public static string FormatWithUnit(double milliseconds, DurationUnit unit, int decimals)
        => Format(milliseconds, unit, decimals) + " " + UnitLabel(unit);

    /// <summary>
    /// Selects a unit from the single value and formats it with the unit suffix (e.g. "1.10 µs").
    /// </summary>
    public static string FormatAuto(double milliseconds, int decimals)
    {
        var unit = SelectUnit(milliseconds);
        return FormatWithUnit(milliseconds, unit, decimals);
    }

    /// <summary>
    /// Formats a millisecond value in the supplied unit, escalating decimals (4 → 6 → 8) so a
    /// small-but-non-zero value never collapses to "0.0000". Used for confidence-interval margins,
    /// which can be far smaller than the column's central values. No unit suffix.
    /// </summary>
    public static string FormatAdaptive(double milliseconds, DurationUnit unit)
    {
        var value = ToUnit(milliseconds, unit);
        if (value == 0) return "0";

        foreach (var decimals in AdaptiveDecimalSteps)
        {
            var formatted = value.ToString("F" + decimals, CultureInfo.InvariantCulture);
            if (!IsAllZero(formatted)) return formatted;
        }

        return "0";
    }

    private static readonly int[] AdaptiveDecimalSteps = { 4, 6, 8 };

    private static DurationUnit PickUnit(double magnitudeMs)
    {
        var nanoseconds = magnitudeMs * NsPerMs;
        if (nanoseconds <= 0) return DurationUnit.Milliseconds;
        if (nanoseconds < 1_000) return DurationUnit.Nanoseconds;
        if (nanoseconds < 1_000_000) return DurationUnit.Microseconds;
        if (nanoseconds < 1_000_000_000) return DurationUnit.Milliseconds;
        return DurationUnit.Seconds;
    }

    private static bool IsAllZero(string formatted)
    {
        foreach (var character in formatted)
        {
            if (character != '0' && character != '.' && character != '-') return false;
        }

        return true;
    }
}
