using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PositiveIntegerAttribute : IntegerAttribute
{
    public PositiveIntegerAttribute(int minimum = 1, int maximum = 2147483647)
        : base(minimum, maximum)
    {
        if (minimum <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimum));
    }
}