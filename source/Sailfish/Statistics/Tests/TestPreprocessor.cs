using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

    public double[] PreprocessWithDownSample(
        double[] rawData,
        bool useInnerQuartile,
        bool downSample,
        [Range(3, int.MaxValue)] int maxArraySize,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        int? seed = null)
    {
        var preDownSampled = Preprocess(rawData, useInnerQuartile);
        return downSample ? DownSampleWithRandomUniform(preDownSampled, maxArraySize, minArraySize, seed) : preDownSampled;
    }


    private static double[] DownSampleWithRandomUniform(double[] inputArray, int maxArraySize, int minArraySize, int? seed = null)
    {
        if (maxArraySize < minArraySize)
        {
            maxArraySize = minArraySize;
        }

        if (inputArray.Length <= maxArraySize)
        {
            return inputArray;
        }

        if (inputArray.Length <= minArraySize)
        {
            return inputArray;
        }

        var rand = seed is not null ? new Random(seed.Value) : new Random();
        var indices = new HashSet<int>();
        while (indices.Count < maxArraySize)
        {
            indices.Add(rand.Next(inputArray.Length));
        }

        var output = new double[maxArraySize];
        var i = 0;
        foreach (var index in indices)
        {
            output[i++] = inputArray[index];
        }

        return output;
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