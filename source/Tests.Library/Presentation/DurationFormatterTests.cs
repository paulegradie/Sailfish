using System;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

public class DurationFormatterTests
{
    // All inputs are milliseconds (Sailfish's canonical stored unit).

    [Theory]
    [InlineData(1.5e-6, DurationUnit.Nanoseconds)]   // 1.5 ns
    [InlineData(0.0011, DurationUnit.Microseconds)]  // 1100 ns == 1.1 µs
    [InlineData(0.5, DurationUnit.Microseconds)]     // 500 µs
    [InlineData(12.0, DurationUnit.Milliseconds)]    // 12 ms
    [InlineData(1500.0, DurationUnit.Seconds)]       // 1.5 s
    public void SelectUnit_picks_a_human_friendly_unit(double milliseconds, DurationUnit expected)
    {
        DurationFormatter.SelectUnit(milliseconds).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.000999, DurationUnit.Nanoseconds)]  // 999 ns -> ns
    [InlineData(0.001, DurationUnit.Microseconds)]    // 1000 ns == 1 µs -> µs
    [InlineData(0.999, DurationUnit.Microseconds)]    // 999 µs -> µs
    [InlineData(1.0, DurationUnit.Milliseconds)]      // 1 ms -> ms
    [InlineData(999.0, DurationUnit.Milliseconds)]    // 999 ms -> ms
    [InlineData(1000.0, DurationUnit.Seconds)]        // 1000 ms == 1 s -> s
    public void SelectUnit_boundaries_keep_values_in_range(double milliseconds, DurationUnit expected)
    {
        DurationFormatter.SelectUnit(milliseconds).ShouldBe(expected);
    }

    [Fact]
    public void SelectUnit_over_a_column_uses_the_largest_magnitude()
    {
        // A tiny CI margin mixed with a µs-scale mean -> the column should be µs (driven by the max).
        DurationFormatter.SelectUnit(new[] { 0.0011, 0.000015, 0.0008 }).ShouldBe(DurationUnit.Microseconds);
    }

    [Fact]
    public void SelectUnit_ignores_zero_nan_and_infinity_and_defaults_to_ms()
    {
        DurationFormatter.SelectUnit(Array.Empty<double>()).ShouldBe(DurationUnit.Milliseconds);
        DurationFormatter.SelectUnit(new[] { 0.0, 0.0 }).ShouldBe(DurationUnit.Milliseconds);
        DurationFormatter.SelectUnit(new[] { double.NaN, double.PositiveInfinity }).ShouldBe(DurationUnit.Milliseconds);
    }

    [Theory]
    [InlineData(DurationUnit.Nanoseconds, "ns")]
    [InlineData(DurationUnit.Microseconds, "µs")]
    [InlineData(DurationUnit.Milliseconds, "ms")]
    [InlineData(DurationUnit.Seconds, "s")]
    public void UnitLabel_is_correct(DurationUnit unit, string expected)
    {
        DurationFormatter.UnitLabel(unit).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.0011, DurationUnit.Microseconds, 2, "1.10")]
    [InlineData(0.0011, DurationUnit.Nanoseconds, 0, "1100")]
    [InlineData(12.0, DurationUnit.Milliseconds, 2, "12.00")]
    [InlineData(1500.0, DurationUnit.Seconds, 2, "1.50")]
    [InlineData(1.5e-6, DurationUnit.Nanoseconds, 2, "1.50")]
    public void Format_renders_value_in_unit(double milliseconds, DurationUnit unit, int decimals, string expected)
    {
        DurationFormatter.Format(milliseconds, unit, decimals).ShouldBe(expected);
    }

    [Fact]
    public void FormatWithUnit_appends_the_label()
    {
        DurationFormatter.FormatWithUnit(0.0011, DurationUnit.Microseconds, 2).ShouldBe("1.10 µs");
    }

    [Theory]
    [InlineData(1.5e-6, 2, "1.50 ns")]   // 1.5 ns
    [InlineData(0.0011, 2, "1.10 µs")]   // 1.1 µs — the headline acceptance case
    [InlineData(12.0, 2, "12.00 ms")]
    [InlineData(1500.0, 2, "1.50 s")]
    public void FormatAuto_selects_unit_and_formats(double milliseconds, int decimals, string expected)
    {
        DurationFormatter.FormatAuto(milliseconds, decimals).ShouldBe(expected);
    }

    [Fact]
    public void FormatAuto_never_renders_a_nonzero_value_as_zero()
    {
        // A sub-microsecond mean must not collapse to "0.000ms".
        var rendered = DurationFormatter.FormatAuto(0.0011, 3);
        rendered.ShouldNotContain("0.000");
        rendered.ShouldBe("1.100 µs");
    }

    [Fact]
    public void FormatAdaptive_escalates_decimals_so_small_values_stay_visible()
    {
        // A 15 ns margin shown within an ms-scale column is 0.000015 ms; a flat F3 would render
        // "0.000", so FormatAdaptive must escalate decimals to keep it visible.
        var rendered = DurationFormatter.FormatAdaptive(0.000015, DurationUnit.Milliseconds);
        rendered.ShouldBe("0.000015");
    }

    [Fact]
    public void FormatAdaptive_uses_four_decimals_when_already_visible()
    {
        // 15 ns inside a µs column is 0.015 µs — visible at the default 4 decimals.
        DurationFormatter.FormatAdaptive(0.000015, DurationUnit.Microseconds).ShouldBe("0.0150");
    }

    [Fact]
    public void FormatAdaptive_returns_zero_for_zero()
    {
        DurationFormatter.FormatAdaptive(0.0, DurationUnit.Microseconds).ShouldBe("0");
    }

    [Theory]
    [InlineData(1.0, DurationUnit.Nanoseconds, 1_000_000.0)]
    [InlineData(1.0, DurationUnit.Microseconds, 1_000.0)]
    [InlineData(1.0, DurationUnit.Milliseconds, 1.0)]
    [InlineData(1000.0, DurationUnit.Seconds, 1.0)]
    public void ToUnit_converts_from_milliseconds(double milliseconds, DurationUnit unit, double expected)
    {
        DurationFormatter.ToUnit(milliseconds, unit).ShouldBe(expected, 1e-9);
    }
}
