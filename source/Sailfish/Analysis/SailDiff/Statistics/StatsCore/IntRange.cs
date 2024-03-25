using System;
using System.Collections;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

[Serializable]
public struct IntRange(int min, int max) :
    IRange<int>,
    IEquatable<IntRange>,
    IEnumerable<int>
{
    private int min = min;
    private int max = max;

    public int Min
    {
        readonly get => min;
        set => min = value;
    }

    public int Max
    {
        readonly get => max;
        set => max = value;
    }

    public static bool operator ==(IntRange range1, IntRange range2)
    {
        return range1.min == range2.min && range1.max == range2.max;
    }

    public static bool operator !=(IntRange range1, IntRange range2)
    {
        return range1.min != range2.min || range1.max != range2.max;
    }

    public readonly bool Equals(IntRange other)
    {
        return this == other;
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is IntRange intRange && this == intRange;
    }

    public readonly override int GetHashCode()
    {
        return (17 * 31 + min.GetHashCode()) * 31 + max.GetHashCode();
    }

    public readonly override string ToString()
    {
        return $"[{(object)min}, {(object)max}]";
    }

    public readonly string ToString(string format, IFormatProvider formatProvider) => $"[{min.ToString(format, formatProvider)}, {max.ToString(format, formatProvider)}]";

    public static implicit operator DoubleRange(IntRange range)
    {
        return new DoubleRange(range.Min, range.Max);
    }

    public static implicit operator Range(IntRange range)
    {
        return new Range(range.Min, range.Max);
    }

    public readonly IEnumerator<int> GetEnumerator()
    {
        for (var i = min; i < max; ++i)
            yield return i;
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        for (var i = min; i < max; ++i)
            yield return i;
    }
}