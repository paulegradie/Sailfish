using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

[Serializable]
public class EmpiricalOptions : IFittingOptions
{
    public EmpiricalOptions()
    {
        SmoothingRule = EmpiricalDistribution.SmoothingRule;
        InPlace = false;
    }

    public SmoothingRule SmoothingRule { get; set; }

    public bool InPlace { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }
}