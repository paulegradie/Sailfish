using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public abstract class DistributionBase : IFormattable
{
    public abstract string ToString(string? format, IFormatProvider? formatProvider);

    public override string ToString()
    {
        return ToString(null, null);
    }

    public abstract object Clone();
}