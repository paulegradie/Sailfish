using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

public interface IPowerAnalysis : ICloneable, IFormattable
{
    DistributionTailSailfish Tail { get; }

    double Power { get; }

    double Size { get; }

    double Effect { get; }
}