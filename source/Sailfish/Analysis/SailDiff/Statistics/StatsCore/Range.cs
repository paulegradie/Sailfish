using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public struct Range(float min, float max) : IFormattable, IEquatable<Range>
{
    public float Min { get; } = min;

    public float Max { get; } = max;

    public static bool operator ==(Range range1, Range range2)
    {
        return range1.Min == (double)range2.Min && range1.Max == (double)range2.Max;
    }

    public static bool operator !=(Range range1, Range range2)
    {
        return range1.Min != (double)range2.Min || range1.Max != (double)range2.Max;
    }

    public readonly bool Equals(Range other)
    {
        return this == other;
    }

    public readonly override bool Equals(object obj)
    {
        return obj is Range range && this == range;
    }

    public readonly override int GetHashCode()
    {
        return (17 * 31 + Min.GetHashCode()) * 31 + Max.GetHashCode();
    }

    public readonly override string ToString()
    {
        return $"[{Min}, {Max}]";
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"[{Min.ToString(format, formatProvider)}, {Max.ToString(format, formatProvider)}]";
    }

    public static implicit operator DoubleRange(Range range)
    {
        return new DoubleRange(range.Min, range.Max);
    }
}