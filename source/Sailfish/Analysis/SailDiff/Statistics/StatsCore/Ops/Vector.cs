using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static class Vector
{
    private static T[] Create<T>(int size, T value)
    {
        var objArray = new T[size];
        for (var index = 0; index < objArray.Length; ++index)
            objArray[index] = value;
        return objArray;
    }

    public static T[] Ones<T>(int size) where T : struct
    {
        var obj = (T)Convert.ChangeType(1, typeof(T));
        return Create(size, obj);
    }

    public static IEnumerable<long> Range(long n)
    {
        var numArray = new long[(int)n];
        for (var index = 0; index < numArray.Length; ++index)
            numArray[index] = index;
        return numArray;
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
}