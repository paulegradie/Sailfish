using System;
using System.Collections.Generic;
using System.Linq;
using Perfolizer.Mathematics.OutlierDetection;

namespace Sailfish.Analysis;

public interface ISailfishOutlierDetector
{
    public ProcessedStatisticalTestData DetectOutliers(IReadOnlyList<double> originalData);
}

public class SailfishOutlierDetector : ISailfishOutlierDetector
{
    public ProcessedStatisticalTestData DetectOutliers(IReadOnlyList<double> originalData)
    {
        if (originalData.Count <= 3) return new ProcessedStatisticalTestData([.. originalData], [.. originalData], Array.Empty<double>(), Array.Empty<double>(), 0);

        var detector = TukeyOutlierDetector.Create(originalData);

        var outliersRemoved = originalData.Where(x => Between(x, detector.UpperFence, detector.LowerFence)).ToArray();
        var lowerOutliers = originalData.Where(x => Below(x, detector.LowerFence)).ToArray();
        var upperOutliers = originalData.Where(x => Above(x, detector.UpperFence)).ToArray();

        return new ProcessedStatisticalTestData([.. originalData], outliersRemoved, lowerOutliers, upperOutliers, lowerOutliers.Length + upperOutliers.Length);
    }

    private static bool Between(double val, double hi, double low)
    {
        return val >= low && val <= hi;
    }

    private static bool Below(double val, double fence)
    {
        return val < fence;
    }

    private static bool Above(double val, double fence)
    {
        return val > fence;
    }
}