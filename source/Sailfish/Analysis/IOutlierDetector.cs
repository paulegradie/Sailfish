using System.Collections.Generic;

namespace Sailfish.Analysis;

/// <summary>
/// Contract for outlier detection with configurable removal strategies.
/// </summary>
public interface IOutlierDetector
{
    /// <summary>
    /// Detects outliers in the provided data and returns processed results according to the chosen strategy.
    /// </summary>
    /// <param name="originalData">Input sample data.</param>
    /// <param name="strategy">Outlier handling strategy. Defaults to RemoveUpper for new configurable flow.</param>
    /// <returns>Processed statistical data including filtered samples and outlier details.</returns>
    ProcessedStatisticalTestData DetectOutliers(
        IReadOnlyList<double> originalData,
        OutlierStrategy strategy = OutlierStrategy.RemoveUpper);
}

