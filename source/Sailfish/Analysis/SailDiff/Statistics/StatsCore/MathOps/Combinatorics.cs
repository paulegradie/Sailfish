using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

internal static class Combinatorics
{
    public static IEnumerable<int[]> Sequences(int length, bool inPlace = false)
    {
        var symbols = new int[length];
        for (var index = 0; index < symbols.Length; ++index)
            symbols[index] = 2;
        return symbols.Sequences(inPlace);
    }

    public static IEnumerable<int[]> Sequences(this int[] symbols, bool inPlace = false)
    {
        var current = new int[symbols.Length];
        for (var index = 0; index < symbols.Length; ++index)
            if (symbols[index] == 0)
                yield break;

        label_5:
        yield return inPlace ? current : (int[])current.Clone();
        for (var index = symbols.Length - 1; index >= 0; --index)
        {
            if (current[index] != symbols[index] - 1)
            {
                ++current[index];
                break;
            }

            if (index == 0)
                yield break;
            current[index] = 0;
        }

        goto label_5;
    }

    public static IEnumerable<T[]> Combinations<T>(this T[] values, int k, bool inPlace = false)
    {
        var length = values.Length;
        var c = new int[k + 3];
        var current = new T[k];
        int j;
        for (j = 1; j <= k; ++j)
            c[j] = j - 1;
        c[k + 1] = length;
        c[k + 2] = 0;
        j = k;
        do
        {
            for (var index = 0; index < current.Length; ++index)
                current[index] = values[c[index + 1]];
            yield return inPlace ? current : (T[])current.Clone();
            int x;
            if (j > 0)
            {
            }
            else if (c[1] + 1 < c[2])
            {
                ++c[1];
                goto label_16;
            }
            else
            {
                j = 2;
            }

            while (true)
            {
                c[j - 1] = j - 2;
                x = c[j] + 1;
                if (x == c[j + 1])
                    ++j;
                else
                    break;
            }

            c[j] = x;
            --j;
        label_16:;
        } while (j < k);
    }
}