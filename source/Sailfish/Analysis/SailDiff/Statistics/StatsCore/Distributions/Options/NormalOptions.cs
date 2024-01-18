using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

public class NormalOptions : IFittingOptions, IComponentOptions
{
    public NormalOptions()
    {
        Regularization = 0.0;
        Diagonal = false;
    }

    public double Regularization { get; set; }

    public bool Diagonal { get; set; }

    public bool Robust { get; set; }

    public bool Shared { get; set; }

    public Action<IDistribution[], double[]> Postprocessing { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }
}