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
    private readonly ConfigurableOutlierDetector configurable = new();

    public ProcessedStatisticalTestData DetectOutliers(IReadOnlyList<double> originalData)
    {
        // Preserve existing behavior of removing both lower and upper outliers
        return configurable.DetectOutliers(originalData, OutlierStrategy.RemoveAll);
    }
}