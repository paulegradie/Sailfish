using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public class StableComparer<T>(Comparison<T> comparison) : IComparer<KeyValuePair<int, T>>
{
    private readonly Comparison<T> comparison = comparison;

    public int Compare(KeyValuePair<int, T> x, KeyValuePair<int, T> y)
    {
        var num = comparison(x.Value, y.Value);
        return num == 0 ? x.Key.CompareTo(y.Key) : num;
    }
}