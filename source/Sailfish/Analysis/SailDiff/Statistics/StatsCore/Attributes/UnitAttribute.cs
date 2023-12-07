using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class UnitAttribute : RangeAttribute
{
    public UnitAttribute()
        : base(0, 1)
    {
    }
}