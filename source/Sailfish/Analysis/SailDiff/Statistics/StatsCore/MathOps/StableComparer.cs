using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

internal class StableComparer<T> : IComparer<KeyValuePair<int, T>>
{
    private readonly Comparison<T> _comparison;

    public StableComparer(Comparison<T> comparison)
    {
        _comparison = comparison;
    }

    public int Compare(KeyValuePair<int, T> x, KeyValuePair<int, T> y)
    {
        var num = _comparison(x.Value, y.Value);
        return num == 0 ? x.Key.CompareTo(y.Key) : num;
    }
}