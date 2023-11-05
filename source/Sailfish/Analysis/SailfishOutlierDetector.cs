using System;
using System.Collections.Generic;
using System.Linq;
using Perfolizer.Mathematics.OutlierDetection;

namespace Sailfish.Analysis;

public interface ISailfishOutlierDetector
{
    public OutlierAnalysis DetectOutliers(IReadOnlyList<double> data);
}

public class SailfishOutlierDetector : ISailfishOutlierDetector
{
    public OutlierAnalysis DetectOutliers(IReadOnlyList<double> data)
    {
        if (data.Count <= 3)
        {
            return new OutlierAnalysis(data.ToArray(), Array.Empty<double>(), Array.Empty<double>(), 0);
        }

        var detector = TukeyOutlierDetector.Create(data);

        var outliersRemoved = data.Where(x => Between(x, detector.UpperFence, detector.LowerFence)).ToArray();
        var lowerOutliers = data.Where(x => Below(x, detector.LowerFence)).ToArray();
        var upperOutliers = data.Where(x => Above(x, detector.UpperFence)).ToArray();

        return new OutlierAnalysis(outliersRemoved, lowerOutliers, upperOutliers, lowerOutliers.Length + upperOutliers.Length);
    }

    private static bool Between(double val, double hi, double low) => val > low && val < hi;
    private static bool Below(double val, double fence) => val < fence;
    private static bool Above(double val, double fence) => val > fence;
}