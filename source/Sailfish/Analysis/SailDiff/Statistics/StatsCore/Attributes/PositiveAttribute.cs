using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class PositiveAttribute : RealAttribute
{
    public PositiveAttribute(double minimum = 5E-324, double maximum = 1.7976931348623157E+308)
        : base(minimum, maximum)
    {
        if (minimum <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(minimum));
    }
}