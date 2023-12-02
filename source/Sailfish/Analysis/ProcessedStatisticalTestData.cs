using System.Collections.Generic;

namespace Sailfish.Analysis;

public record ProcessedStatisticalTestData(
    double[] OriginalData,
    double[] DataWithOutliersRemoved,
    IEnumerable<double> LowerOutliers,
    IEnumerable<double> UpperOutliers,
    int TotalNumOutliers);