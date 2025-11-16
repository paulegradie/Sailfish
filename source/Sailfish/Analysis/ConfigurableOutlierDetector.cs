using System;
using System.Collections.Generic;
using System.Linq;
using Perfolizer.Mathematics.OutlierDetection;

namespace Sailfish.Analysis;

/// <summary>
/// Outlier detector that supports multiple removal strategies backed by Tukey fences.
/// </summary>
public class ConfigurableOutlierDetector : IOutlierDetector
{
    public ProcessedStatisticalTestData DetectOutliers(
        IReadOnlyList<double> originalData,
        OutlierStrategy strategy = OutlierStrategy.RemoveUpper)
    {
        if (originalData is null || originalData.Count == 0)
        {
            return new ProcessedStatisticalTestData(Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>(), 0);
        }

        // For very small N, do not attempt to detect/remove outliers
        if (originalData.Count <= 3)
        {
            return new ProcessedStatisticalTestData([.. originalData], [.. originalData], Array.Empty<double>(), Array.Empty<double>(), 0);
        }

        var detector = TukeyOutlierDetector.Create(originalData);

        // Identify outliers according to Tukey fences
        var lowerOutliers = originalData.Where(x => Below(x, detector.LowerFence)).ToArray();
        var upperOutliers = originalData.Where(x => Above(x, detector.UpperFence)).ToArray();

        // Determine effective strategy for Adaptive
        var effective = strategy == OutlierStrategy.Adaptive
            ? ChooseAdaptive(lowerOutliers.Length, upperOutliers.Length)
            : strategy;

        var filtered = effective switch
        {
            OutlierStrategy.DontRemove => [.. originalData],
            OutlierStrategy.RemoveUpper => originalData.Where(x => !Above(x, detector.UpperFence)).ToArray(),
            OutlierStrategy.RemoveLower => originalData.Where(x => !Below(x, detector.LowerFence)).ToArray(),
            OutlierStrategy.RemoveAll => originalData.Where(x => Between(x, detector.UpperFence, detector.LowerFence)).ToArray(),
            _ => [.. originalData]
        };

        var total = lowerOutliers.Length + upperOutliers.Length;
        return new ProcessedStatisticalTestData([.. originalData], filtered, lowerOutliers, upperOutliers, total);
    }

    private static OutlierStrategy ChooseAdaptive(int lowerCount, int upperCount)
    {
        if (upperCount > 0 && lowerCount == 0) return OutlierStrategy.RemoveUpper;
        if (lowerCount > 0 && upperCount == 0) return OutlierStrategy.RemoveLower;
        if (upperCount + lowerCount >= 2) return OutlierStrategy.RemoveAll;
        // Default bias toward removing upper outliers in ambiguous cases
        return OutlierStrategy.RemoveUpper;
    }

    private static bool Between(double val, double hi, double low) => val >= low && val <= hi;
    private static bool Below(double val, double fence) => val < fence;
    private static bool Above(double val, double fence) => val > fence;
}

