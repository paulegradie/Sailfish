using System.Collections.Generic;

namespace Sailfish.Analysis;

public interface ISailfishOutlierDetector
{
    public ProcessedStatisticalTestData DetectOutliers(IReadOnlyList<double> originalData);
}

public class SailfishOutlierDetector : ISailfishOutlierDetector
{
    private readonly ConfigurableOutlierDetector _configurable = new();

    public ProcessedStatisticalTestData DetectOutliers(IReadOnlyList<double> originalData)
    {
        // Preserve existing behavior of removing both lower and upper outliers
        return _configurable.DetectOutliers(originalData, OutlierStrategy.RemoveAll);
    }
}