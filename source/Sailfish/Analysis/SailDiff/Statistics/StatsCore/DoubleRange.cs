namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

internal record DoubleRange
{
    public DoubleRange(double Min, double Max)
    {
        this.Min = Min;
        this.Max = Max;
    }

    public double Min { get; init; }
    public double Max { get; init; }

    public void Deconstruct(out double Min, out double Max)
    {
        Min = this.Min;
        Max = this.Max;
    }
}