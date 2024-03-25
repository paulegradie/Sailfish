namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

public abstract class IndependentOptions : IFittingOptions
{
    public virtual object Clone()
    {
        return MemberwiseClone();
    }
}