using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Utils.MathOps;

public static class ChooseWeighted
{
    private const double Tolerance = 0.001;
    private static readonly Random Random = new();

    public static T Choose<T>(List<T> list) where T : IHaveProbabilities
    {
        if (list.Count == 0)
        {
            throw new Exception("List to choose from cannot be empty");
        }

        var probSum = Math.Abs(1.0 - list.Sum(i => i.Probability));
        if (probSum > Tolerance)
        {
            throw new Exception($"Probabilities must sum to 1. {probSum}");
        }

        var sum = 0;
        var weightSum = ComputeTotalWeightSum(list);
        var choice = Random.Next((int)weightSum);

        foreach (var selectionItem in list)
        {
            var scaledProbabilityAsInt = ConvertProbabilityToInt(selectionItem.Probability);
            for (var i = sum; i < scaledProbabilityAsInt + sum; i++)
            {
                if (i >= choice)
                {
                    return selectionItem;
                }
            }

            sum += scaledProbabilityAsInt;
        }

        return list.First();
    }

    private static double ComputeTotalWeightSum<T>(IEnumerable<T> weights) where T : IHaveProbabilities
    {
        return weights.Sum(w => w.Probability * 100);
    }

    private static int ConvertProbabilityToInt(double probability) => (int)(probability * 100);
}