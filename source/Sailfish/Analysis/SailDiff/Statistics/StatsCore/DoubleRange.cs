using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public struct DoubleRange(double min, double max) : IFormattable, IEquatable<DoubleRange>
{
    public double Min { get; set; } = min;

    public double Max { get; set; } = max;

    public static bool operator ==(DoubleRange range1, DoubleRange range2)
    {
        return range1.Min == range2.Min && range1.Max == range2.Max;
    }

    public static bool operator !=(DoubleRange range1, DoubleRange range2)
    {
        return range1.Min != range2.Min || range1.Max != range2.Max;
    }

    public readonly bool Equals(DoubleRange other)
    {
        return this == other;
    }

    public readonly override bool Equals(object obj)
    {
        return obj is DoubleRange doubleRange && this == doubleRange;
    }

    public readonly override int GetHashCode()
    {
        return (17 * 31 + Min.GetHashCode()) * 31 + Max.GetHashCode();
    }

    public readonly override string ToString()
    {
        return $"[{Min}, {Max}]";
    }

    public readonly string ToString(string format, IFormatProvider formatProvider)
    {
        return $"[{Min.ToString(format, formatProvider)}, {Max.ToString(format, formatProvider)}]";
    }
}