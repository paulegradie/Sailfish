using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests;

public record PreprocessedData(double[] RawData, ProcessedStatisticalTestData? OutlierAnalysis);

public interface ITestPreprocessor
{
    PreprocessedData Preprocess(double[] input, bool useOutlierDetection);

    PreprocessedData PreprocessWithDownSample(
        double[] rawData,
        bool useOutlierDetection,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        [Range(3, int.MaxValue)] int maxArraySize = 10,
        int? seed = null);

    (PreprocessedData, PreprocessedData) PreprocessJointlyWithDownSample(
        double[] sample1,
        double[] sample2,
        bool useOutlierDetection,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        [Range(3, int.MaxValue)] int maxArraySize = 10,
        int? seed = null);
}

public class TestPreprocessor(ISailfishOutlierDetector outlierDetector) : ITestPreprocessor
{
    private const int MinAnalysisSampSize = 3;
    private readonly ISailfishOutlierDetector outlierDetector = outlierDetector;

    public PreprocessedData Preprocess(double[] rawData, bool useOutlierDetection)
    {
        if (!useOutlierDetection || rawData.Length < MinAnalysisSampSize) return new PreprocessedData(rawData, null);

        var outlierAnalysis = outlierDetector.DetectOutliers(rawData);
        return new PreprocessedData(rawData, outlierAnalysis);
    }

    public PreprocessedData PreprocessWithDownSample(double[] rawData,
        bool useOutlierDetection,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        [Range(3, int.MaxValue)] int maxArraySize = 10,
        int? seed = null)
    {
        if (useOutlierDetection)
        {
            var outlierAnalysis = outlierDetector.DetectOutliers(rawData);
            var downSampled = DownSampleWithRandomUniform(outlierAnalysis.DataWithOutliersRemoved, minArraySize, maxArraySize, seed);
            return new PreprocessedData(rawData,
                outlierAnalysis with { OriginalData = rawData, DataWithOutliersRemoved = downSampled });
        }

        var downSampledNoOutlierDetection = DownSampleWithRandomUniform(rawData, minArraySize, maxArraySize, seed);
        return new PreprocessedData(downSampledNoOutlierDetection, null);
    }

    public (PreprocessedData, PreprocessedData) PreprocessJointlyWithDownSample(
        double[] sample1,
        double[] sample2,
        bool useOutlierDetection,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        [Range(3, int.MaxValue)] int maxArraySize = 10,
        int? seed = null)
    {
        if (useOutlierDetection)
        {
            var sample1OutlierAnalysis = outlierDetector.DetectOutliers(sample1);
            var sample2OutlierAnalysis = outlierDetector.DetectOutliers(sample2);

            var smallestArray = Math.Min(sample1OutlierAnalysis.DataWithOutliersRemoved.Length, sample2OutlierAnalysis.DataWithOutliersRemoved.Length);
            var adjustedMax = Math.Min(smallestArray, maxArraySize);

            var downSample1 = DownSampleWithRandomUniform(sample1OutlierAnalysis.DataWithOutliersRemoved, minArraySize, adjustedMax, seed);
            var downSample2 = DownSampleWithRandomUniform(sample2OutlierAnalysis.DataWithOutliersRemoved, minArraySize, adjustedMax, seed);

            var preprocessed1 = new PreprocessedData(sample1, sample1OutlierAnalysis with { DataWithOutliersRemoved = downSample1 });
            var preprocessed2 = new PreprocessedData(sample2, sample2OutlierAnalysis with { DataWithOutliersRemoved = downSample2 });

            return (preprocessed1, preprocessed2);
        }

        var smallestArrayNoAnalysis = Math.Min(sample1.Length, sample2.Length);
        var adjustedMaxNoAnalysis = Math.Min(smallestArrayNoAnalysis, maxArraySize);

        var downSampled1NoOutlierDetection = DownSampleWithRandomUniform(sample1, minArraySize, adjustedMaxNoAnalysis, seed);
        var downSampled2NoOutlierDetection = DownSampleWithRandomUniform(sample2, minArraySize, adjustedMaxNoAnalysis, seed);
        return (new PreprocessedData(downSampled1NoOutlierDetection, null), new PreprocessedData(downSampled2NoOutlierDetection, null));
    }

    private static double[] DownSampleWithRandomUniform(double[] inputArray, int minArraySize, int maxArraySize, int? seed = null)
    {
        if (maxArraySize < minArraySize) maxArraySize = minArraySize;

        if (inputArray.Length <= maxArraySize) return inputArray;

        if (inputArray.Length <= minArraySize) return inputArray;

        var rand = seed is not null ? new Random(seed.Value) : new Random();
        var indices = new HashSet<int>();
        while (indices.Count < maxArraySize) indices.Add(rand.Next(inputArray.Length));

        var output = new double[maxArraySize];
        var i = 0;
        foreach (var index in indices) output[i++] = inputArray[index];

        return output;
    }
}