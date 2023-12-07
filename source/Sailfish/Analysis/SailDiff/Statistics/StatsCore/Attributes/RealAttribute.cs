using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
internal class RealAttribute(double minimum = -1.7976931348623157E+308, double maximum = 1.7976931348623157E+308) : RangeAttribute(minimum, maximum)
{
}