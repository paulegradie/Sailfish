using System.Collections.Generic;

namespace Sailfish.Analysis;

public record OutlierAnalysis(
    double[] DataWithOutliersRemoved,
    IEnumerable<double> LowerOutliers,
    IEnumerable<double> UpperOutliers,
    int TotalNumOutliers);