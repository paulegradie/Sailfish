using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

public static class Tools
{
    public static double[] Rank(
        this double[] samples,
        bool alreadySorted = false,
        bool adjustForTies = true)
    {
        var numberArray = Vector.Range(0, samples.Length);
        if (!alreadySorted)
        {
            samples = (double[])samples.Clone();
            Array.Sort(samples, numberArray);
        }

        var items = new double[samples.Length];
        var doubleCounter = 0.0;
        var altCounter = 0;
        if (samples.Length == 0) return [];

        items[0] = 1.0;
        if (adjustForTies)
        {
            var counter = 1;
            for (var index1 = 1; index1 < items.Length; ++index1)
                if (Math.Abs(samples[index1] - samples[index1 - 1]) > 0.000000000001)
                {
                    if (altCounter > 0)
                    {
                        for (var index2 = 0; index2 < altCounter + 1; ++index2)
                        {
                            var index3 = index1 - index2 - 1;
                            items[index3] = (counter + doubleCounter) / (altCounter + 1.0);
                        }

                        altCounter = 0;
                        doubleCounter = 0.0;
                    }

                    items[index1] = ++counter;
                }
                else
                {
                    ++altCounter;
                    doubleCounter += counter++;
                }

            if (altCounter > 0)
                for (var index4 = 0; index4 < altCounter + 1; ++index4)
                {
                    var index5 = samples.Length - index4 - 1;
                    items[index5] = (counter + doubleCounter) / (altCounter + 1.0);
                }
        }
        else
        {
            var index = 1;
            var i = 1;
            for (; index < items.Length; ++index) items[index] = ++i;
        }

        if (!alreadySorted)
            Array.Sort((Array)numberArray, items);
        return items;
    }

    public static IEnumerable<int> Ties(this IEnumerable<double> ranks)
    {
        SortedDictionary<double, int> counts = [];
        foreach (var rank in ranks)
        {
            var countNumber = counts.GetValueOrDefault(rank, 0);
            counts[rank] = countNumber + 1;
        }

        var numberArray1 = new int[counts.Count];
        var numberArray2 = counts.Keys.Sorted();
        for (var index = 0; index < numberArray2.Length; ++index)
            numberArray1[index] = counts[numberArray2[index]];
        return numberArray1;
    }
}