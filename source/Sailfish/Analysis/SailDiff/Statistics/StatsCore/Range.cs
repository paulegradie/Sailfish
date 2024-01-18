using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public struct Range(float min, float max) : IRange<float>, IEquatable<Range>
{
    public float Min { get; set; } = min;

    public float Max { get; set; } = max;

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

    public override readonly bool Equals(object obj)
    {
        return obj is Range range && this == range;
    }

    public override readonly int GetHashCode()
    {
        return (17 * 31 + Min.GetHashCode()) * 31 + Max.GetHashCode();
    }

    public override readonly string ToString()
    {
        return string.Format("[{0}, {1}]", Min, Max);
    }

    public readonly string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format("[{0}, {1}]", Min.ToString(format, formatProvider), Max.ToString(format, formatProvider));
    }

    public static implicit operator DoubleRange(Range range)
    {
        return new DoubleRange(range.Min, range.Max);
    }
}