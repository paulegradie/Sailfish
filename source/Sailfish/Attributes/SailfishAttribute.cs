using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class SailfishAttribute : Attribute
{
    private const int DefaultNumIterations = 3;
    private const int DefaultNumWarmupIterations = 3;
    
    internal SailfishAttribute()
    {
    }

    /// <summary>
    /// Attribute to placed on a Sailfish test class.  
    /// See: <a href="https://paulgradie.com/Sailfish/docs/2/the-sailfish-attribute">The Sailfish Attributes</a>
    /// </summary>
    /// <param name="numIterations">Number of times each SailfishMethod will be iterated</param>
    /// <param name="numWarmupIterations">Number of times each SailfishMethod will be iterated without being timed before executing numIterations with tracking</param>
    /// <remarks>For both numIterations and numWarmupIterations, each iteration is an invocation of three methods: SailfishIterationSetup, SailfishMethod, and SailfishIterationTeardown methods - in that order</remarks>
    public SailfishAttribute(
        [Range(2, int.MaxValue)] int numIterations = DefaultNumIterations, 
        [Range(0, int.MaxValue)] int numWarmupIterations = DefaultNumWarmupIterations)
    {
        NumIterations = numIterations;
        NumWarmupIterations = numWarmupIterations;
    }

    [Range(2, int.MaxValue)] public int NumIterations { get; set; }
    [Range(0, int.MaxValue)] public int NumWarmupIterations { get; set; }
    public bool Disabled { get; set; }
}