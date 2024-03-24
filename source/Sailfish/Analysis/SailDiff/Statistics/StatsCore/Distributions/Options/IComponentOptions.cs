using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

public interface IComponentOptions : IFittingOptions
{
    Action<IDistribution[], double[]> Postprocessing { get; set; }
}