using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SailfishAttribute : Attribute
{
    internal SailfishAttribute()
    {
    }

    public SailfishAttribute([Range(2, int.MaxValue)]int numIterations = 3, [Range(0, int.MaxValue)]int numWarmupIterations = 3)
    {
        NumIterations = numIterations;
        NumWarmupIterations = numWarmupIterations;
    }

    public int NumIterations { get; set; } = 1;
    public int NumWarmupIterations { get; set; } = 1;
    public bool Disabled { get; set; }
}