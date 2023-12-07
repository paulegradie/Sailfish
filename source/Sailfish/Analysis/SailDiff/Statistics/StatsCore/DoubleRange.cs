using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public struct DoubleRange(double min, double max) : IRange<double>, IEquatable<DoubleRange>
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

    public override readonly bool Equals(object obj)
    {
        return obj is DoubleRange doubleRange && this == doubleRange;
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
}