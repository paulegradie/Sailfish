using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
internal class RealAttribute : RangeAttribute
{
    public RealAttribute(double minimum = -1.7976931348623157E+308, double maximum = 1.7976931348623157E+308) : base(minimum, maximum)
    {
    }
}