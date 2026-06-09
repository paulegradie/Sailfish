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
        // Trim the upper tail only. In a benchmark the slow samples are the contaminated ones (a GC
        // pause, a context switch, a page fault); the fast samples are the cleanest estimate of the true
        // cost, so removing the lower tail (the old RemoveAll behavior) discarded the best data and biased
        // the central estimate upward. Using one strategy here also keeps the per-run summary and the
        // SailDiff comparison consistent, since both resolve this same detector.
        return _configurable.DetectOutliers(originalData, OutlierStrategy.RemoveUpper);
    }
}