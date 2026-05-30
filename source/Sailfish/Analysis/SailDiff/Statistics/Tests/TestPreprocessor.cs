using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests;

public record PreprocessedData
{
    public PreprocessedData(double[] RawData, ProcessedStatisticalTestData? OutlierAnalysis)
    {
        this.RawData = RawData;
        this.OutlierAnalysis = OutlierAnalysis;
    }

    public double[] RawData { get; init; }
    public ProcessedStatisticalTestData? OutlierAnalysis { get; init; }

    public void Deconstruct(out double[] RawData, out ProcessedStatisticalTestData? OutlierAnalysis)
    {
        RawData = this.RawData;
        OutlierAnalysis = this.OutlierAnalysis;
    }
}

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

public class TestPreprocessor : ITestPreprocessor
{
    private const int MinAnalysisSampSize = 3;
    private readonly ISailfishOutlierDetector _outlierDetector;
    private readonly IRunSettings? _runSettings;

    public TestPreprocessor(ISailfishOutlierDetector outlierDetector)
    {
        _outlierDetector = outlierDetector;
        _runSettings = null;
    }

    /// <summary>
    /// Preferred ctor: when an explicit seed is not passed to the down-sample methods, falls back
    /// to <see cref="IRunSettings.Seed"/>. Without this, stats would be non-deterministic even
    /// when the user has configured a seed via <c>RunSettingsBuilder.WithSeed</c>.
    /// </summary>
    public TestPreprocessor(ISailfishOutlierDetector outlierDetector, IRunSettings runSettings)
        : this(outlierDetector)
    {
        _runSettings = runSettings;
    }

    public PreprocessedData Preprocess(double[] rawData, bool useOutlierDetection)
    {
        if (!useOutlierDetection || rawData.Length < MinAnalysisSampSize) return new PreprocessedData(rawData, null);

        var outlierAnalysis = _outlierDetector.DetectOutliers(rawData);
        return new PreprocessedData(rawData, outlierAnalysis);
    }

    public PreprocessedData PreprocessWithDownSample(double[] rawData,
        bool useOutlierDetection,
        [Range(3, int.MaxValue)] int minArraySize = 3,
        [Range(3, int.MaxValue)] int maxArraySize = 10,
        int? seed = null)
    {
        var effectiveSeed = seed ?? _runSettings?.Seed;
        if (useOutlierDetection)
        {
            var outlierAnalysis = _outlierDetector.DetectOutliers(rawData);
            var downSampled = DownSampleWithRandomUniform(outlierAnalysis.DataWithOutliersRemoved, minArraySize, maxArraySize, effectiveSeed);
            return new PreprocessedData(rawData,
                outlierAnalysis with { OriginalData = rawData, DataWithOutliersRemoved = downSampled });
        }

        var downSampledNoOutlierDetection = DownSampleWithRandomUniform(rawData, minArraySize, maxArraySize, effectiveSeed);
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
        var effectiveSeed = seed ?? _runSettings?.Seed;
        if (useOutlierDetection)
        {
            var sample1OutlierAnalysis = _outlierDetector.DetectOutliers(sample1);
            var sample2OutlierAnalysis = _outlierDetector.DetectOutliers(sample2);

            var smallestArray = Math.Min(sample1OutlierAnalysis.DataWithOutliersRemoved.Length, sample2OutlierAnalysis.DataWithOutliersRemoved.Length);
            var adjustedMax = Math.Min(smallestArray, maxArraySize);

            // Use different seed streams for the two samples so a single configured seed still
            // produces independent draws; without offsetting, sample1 and sample2 would be drawn
            // from the same index set whenever they're the same length.
            var downSample1 = DownSampleWithRandomUniform(sample1OutlierAnalysis.DataWithOutliersRemoved, minArraySize, adjustedMax, effectiveSeed);
            var downSample2 = DownSampleWithRandomUniform(sample2OutlierAnalysis.DataWithOutliersRemoved, minArraySize, adjustedMax, OffsetSeed(effectiveSeed));

            var preprocessed1 = new PreprocessedData(sample1, sample1OutlierAnalysis with { DataWithOutliersRemoved = downSample1 });
            var preprocessed2 = new PreprocessedData(sample2, sample2OutlierAnalysis with { DataWithOutliersRemoved = downSample2 });

            return (preprocessed1, preprocessed2);
        }

        var smallestArrayNoAnalysis = Math.Min(sample1.Length, sample2.Length);
        var adjustedMaxNoAnalysis = Math.Min(smallestArrayNoAnalysis, maxArraySize);

        var downSampled1NoOutlierDetection = DownSampleWithRandomUniform(sample1, minArraySize, adjustedMaxNoAnalysis, effectiveSeed);
        var downSampled2NoOutlierDetection = DownSampleWithRandomUniform(sample2, minArraySize, adjustedMaxNoAnalysis, OffsetSeed(effectiveSeed));
        return (new PreprocessedData(downSampled1NoOutlierDetection, null), new PreprocessedData(downSampled2NoOutlierDetection, null));
    }

    private static int? OffsetSeed(int? seed) => seed is null ? null : unchecked(seed.Value + 0x5BD1E995);

    private static double[] DownSampleWithRandomUniform(double[] inputArray, int minArraySize, int maxArraySize, int? seed = null)
    {
        if (maxArraySize < minArraySize) maxArraySize = minArraySize;

        if (inputArray.Length <= maxArraySize) return inputArray;

        if (inputArray.Length <= minArraySize) return inputArray;

        var rand = seed is not null ? new Random(seed.Value) : new Random();
        var indices = new HashSet<int>();
        while (indices.Count < maxArraySize) indices.Add(rand.Next(inputArray.Length));

        // Sort indices for deterministic output ordering — HashSet<int>'s enumeration order is
        // not part of its contract, so iterating directly leaves the output index sequence
        // implementation-defined even with a fixed seed.
        var orderedIndices = new int[indices.Count];
        indices.CopyTo(orderedIndices);
        Array.Sort(orderedIndices);

        var output = new double[maxArraySize];
        for (var i = 0; i < orderedIndices.Length; i++) output[i] = inputArray[orderedIndices[i]];

        return output;
    }
}
