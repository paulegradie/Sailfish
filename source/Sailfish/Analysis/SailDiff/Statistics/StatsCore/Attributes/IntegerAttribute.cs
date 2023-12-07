using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class IntegerAttribute(int minimum = -2147483648, int maximum = 2147483647) : RangeAttribute(minimum, maximum)
{
}