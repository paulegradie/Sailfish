namespace Sailfish.Statistics.StatisticalAnalysis;

public class TTesetCsvRecord
{
    public string TestName { get; set; } = null!;
    public string MeanOfBefore { get; set; } = null!;
    public string MeanOfAfter { get; set; } = null!;
    public string PValue { get; set; } = null!;
    public string DegreesOfFreedom { get; set; } = null!;
    public string TStatistic { get; set; } = null!;
}
