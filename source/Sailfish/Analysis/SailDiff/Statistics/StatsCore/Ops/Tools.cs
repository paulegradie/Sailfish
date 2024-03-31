using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static class Tools
{
    public static double[] Rank(
        this double[] samples,
        bool alreadySorted = false,
        bool adjustForTies = true)
    {
        var numArray = Vector.Range(0, samples.Length);
        if (!alreadySorted)
        {
            samples = (double[])samples.Clone();
            Array.Sort(samples, numArray);
        }

        var items = new double[samples.Length];
        var num1 = 0.0;
        var num2 = 0;
        if (samples.Length == 0)
            return [];
        items[0] = 1.0;
        if (adjustForTies)
        {
            var num3 = 1;
            for (var index1 = 1; index1 < items.Length; ++index1)
                if (samples[index1] != samples[index1 - 1])
                {
                    if (num2 > 0)
                    {
                        for (var index2 = 0; index2 < num2 + 1; ++index2)
                        {
                            var index3 = index1 - index2 - 1;
                            items[index3] = (num3 + num1) / (num2 + 1.0);
                        }

                        num2 = 0;
                        num1 = 0.0;
                    }

                    items[index1] = ++num3;
                }
                else
                {
                    ++num2;
                    num1 += num3++;
                }

            if (num2 > 0)
                for (var index4 = 0; index4 < num2 + 1; ++index4)
                {
                    var index5 = samples.Length - index4 - 1;
                    items[index5] = (num3 + num1) / (num2 + 1.0);
                }
        }
        else
        {
            var index = 1;
            var num4 = 1;
            for (; index < items.Length; ++index)
            {
                items[index] = ++num4;
            }
        }

        if (!alreadySorted)
            Array.Sort((Array)numArray, items);
        return items;
    }

    public static IEnumerable<int> Ties(this double[] ranks)
    {
        SortedDictionary<double, int> counts;
        counts = [];
        foreach (var rank in ranks)
        {
            var num = counts.GetValueOrDefault(rank, 0);
            counts[rank] = num + 1;
        }

        var numArray1 = new int[counts.Count];
        var numArray2 = counts.Keys.Sorted();
        for (var index = 0; index < numArray2.Length; ++index)
            numArray1[index] = counts[numArray2[index]];
        return numArray1;
    }
}