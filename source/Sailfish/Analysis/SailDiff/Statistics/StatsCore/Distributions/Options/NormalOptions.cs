using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

public class NormalOptions : IComponentOptions
{
    public NormalOptions()
    {
        Regularization = 0.0;
        Diagonal = false;
    }

    public double Regularization { get; }

    public bool Diagonal { get; }

    public bool Robust { get; }

    public bool Shared { get; }

    public Action<IDistribution[], double[]> Postprocessing { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }
}