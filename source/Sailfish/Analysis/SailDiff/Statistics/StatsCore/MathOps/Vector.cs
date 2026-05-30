using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

public static class Vector
{
    /// <summary>
    /// Lazy <c>[0, n)</c> generator over <see cref="long"/>. Pre-Tier-2 this eagerly
    /// allocated a <c>long[n]</c> (cast to <c>(int)n</c> — a latent overflow for huge n)
    /// even though it was only ever consumed as <see cref="IEnumerable{T}"/> sequentially.
    /// Now a true generator: zero allocations beyond the enumerator.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="n"/> is negative. The pre-Tier-2 eager implementation
    /// failed fast (via <c>new long[(int)n]</c>); the lazy form preserves that contract so
    /// invalid calls aren't masked as empty sequences.
    /// </exception>
    public static IEnumerable<long> Range(long n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n), "Count must be non-negative.");

        for (long i = 0; i < n; ++i)
            yield return i;
    }

    public static int[] Range(int a, int b)
    {
        if (a == b)
            return [];
        int[] numArray;
        if (b > a)
        {
            numArray = new int[b - a];
            for (var index = 0; index < numArray.Length; ++index)
                numArray[index] = a++;
        }
        else
        {
            numArray = new int[a - b];
            for (var index = 0; index < numArray.Length; ++index)
                numArray[index] = a--;
        }

        return numArray;
    }

    public static T[] Sorted<T>(this ICollection<T> values, bool stable = false) where T : IComparable<T>
    {
        var objArray = new T[values.Count];
        values.CopyTo(objArray, 0);
        objArray.Sort(stable);
        return objArray;
    }

    private static void Sort<T>(this T[] values, bool stable = false, bool asc = true) where T : IComparable<T>
    {
        if (!stable)
        {
            Array.Sort(values);
        }
        else
        {
            var keys = new KeyValuePair<int, T>[values.Length];
            for (var key = 0; key < values.Length; ++key)
                keys[key] = new KeyValuePair<int, T>(key, values[key]);
            Array.Sort(keys, values, new StableComparer<T>((a, b) => a.CompareTo(b)));
        }

        if (asc)
            return;
        Array.Reverse((Array)values);
    }

    public static T[] Get<T>(this T[] source, int[] indexes, bool inPlace = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(indexes);
        if (inPlace && source.Length != indexes.Length)
            throw new DimensionMismatchException("Source and indexes arrays must have the same dimension for in-place operations.");
        var objArray = new T[indexes.Length];
        for (var index1 = 0; index1 < indexes.Length; ++index1)
        {
            var index2 = indexes[index1];
            objArray[index1] = index2 < 0 ? source[source.Length + index2] : source[index2];
        }

        if (!inPlace) return objArray;

        for (var index = 0; index < objArray.Length; ++index) source[index] = objArray[index];

        return objArray;
    }
}