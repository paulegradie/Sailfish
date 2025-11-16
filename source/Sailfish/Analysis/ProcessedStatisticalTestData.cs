using System.Collections.Generic;

namespace Sailfish.Analysis;

public record ProcessedStatisticalTestData
{
    public ProcessedStatisticalTestData(double[] OriginalData,
        double[] DataWithOutliersRemoved,
        IEnumerable<double> LowerOutliers,
        IEnumerable<double> UpperOutliers,
        int TotalNumOutliers)
    {
        this.OriginalData = OriginalData;
        this.DataWithOutliersRemoved = DataWithOutliersRemoved;
        this.LowerOutliers = LowerOutliers;
        this.UpperOutliers = UpperOutliers;
        this.TotalNumOutliers = TotalNumOutliers;
    }

    public double[] OriginalData { get; init; }
    public double[] DataWithOutliersRemoved { get; init; }
    public IEnumerable<double> LowerOutliers { get; init; }
    public IEnumerable<double> UpperOutliers { get; init; }
    public int TotalNumOutliers { get; init; }

    public void Deconstruct(out double[] OriginalData, out double[] DataWithOutliersRemoved, out IEnumerable<double> LowerOutliers, out IEnumerable<double> UpperOutliers, out int TotalNumOutliers)
    {
        OriginalData = this.OriginalData;
        DataWithOutliersRemoved = this.DataWithOutliersRemoved;
        LowerOutliers = this.LowerOutliers;
        UpperOutliers = this.UpperOutliers;
        TotalNumOutliers = this.TotalNumOutliers;
    }
}