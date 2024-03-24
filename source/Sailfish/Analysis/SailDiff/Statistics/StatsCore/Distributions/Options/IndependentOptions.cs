using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

public class IndependentOptions : IFittingOptions
{
    public IFittingOptions InnerOption { get; set; }

    public IFittingOptions[] InnerOptions { get; set; }

    public bool Transposed { get; set; }

    public virtual object Clone()
    {
        return MemberwiseClone();
    }
}