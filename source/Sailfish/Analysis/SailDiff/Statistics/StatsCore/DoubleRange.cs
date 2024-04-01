using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

internal readonly struct DoubleRange(double min, double max) : IFormattable, IEquatable<DoubleRange>
{
    public double Min { get; } = min;

    public double Max { get; } = max;

    public static bool operator ==(DoubleRange range1, DoubleRange range2)
    {
        return range1.Min == range2.Min && range1.Max == range2.Max;
    }

    public static bool operator !=(DoubleRange range1, DoubleRange range2)
    {
        return range1.Min != range2.Min || range1.Max != range2.Max;
    }

    public bool Equals(DoubleRange other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is DoubleRange doubleRange && this == doubleRange;
    }

    public override int GetHashCode()
    {
        return (17 * 31 + Min.GetHashCode()) * 31 + Max.GetHashCode();
    }

    public override string ToString()
    {
        return $"[{Min}, {Max}]";
    }

    public string ToString(string? format, IFormatProvider formatProvider)
    {
        return $"[{Min.ToString(format, formatProvider)}, {Max.ToString(format, formatProvider)}]";
    }
}