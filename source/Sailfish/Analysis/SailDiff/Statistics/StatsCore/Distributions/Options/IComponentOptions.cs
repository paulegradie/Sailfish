using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

public interface IComponentOptions : IFittingOptions, ICloneable
{
    Action<IDistribution[], double[]> Postprocessing { get; set; }
}