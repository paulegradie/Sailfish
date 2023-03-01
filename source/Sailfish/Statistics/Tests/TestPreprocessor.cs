using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using Sailfish.MathOps;

namespace Sailfish.Statistics.Tests;

public class TestPreprocessor : ITestPreprocessor
{
    private const int MinimumSampleSizeForTruncation = 10;
    private const double Shift = 0.00000001;

    public double[] Preprocess(double[] rawData, bool useInnerQuartile)
    {
        if (!useInnerQuartile) return AddNoiseIfNecessary(rawData);
        if (rawData.Length < MinimumSampleSizeForTruncation) return AddNoiseIfNecessary(rawData);
        var quartiles = ComputeQuartiles.GetInnerQuartileValues(rawData);
        return AddNoiseIfNecessary(quartiles);
    }

    private static double[] AddNoiseIfNecessary(double[] array)
    {
        return array.StandardDeviation() == 0 ? AddNoise(array) : array;
    }

    private static double[] AddNoise(IReadOnlyList<double> array)
    {
        var rand = new Random();
        var noisyArray = new double[array.Count];
        for (var i = 0; i < array.Count; i++)
        {
            var noise = rand.NextDouble() * Shift;
            noisyArray[i] = array[i] + noise;
        }

        return noisyArray;
    }
}