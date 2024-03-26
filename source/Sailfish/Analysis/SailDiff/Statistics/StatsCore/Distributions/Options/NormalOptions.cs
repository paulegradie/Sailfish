using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

public class NormalOptions : ICloneable
{
    public NormalOptions()
    {
        Regularization = 0.0;
        Diagonal = false;
    }

    public double Regularization { get; }

    public bool Diagonal { get; }

    public bool Robust { get; }

    public object Clone()
    {
        return MemberwiseClone();
    }
}