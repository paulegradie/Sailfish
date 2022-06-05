using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VeerPerforma.Attributes.TestHarness;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class VeerPerformaAttribute : Attribute
{
    internal VeerPerformaAttribute()
    {
    }

    public VeerPerformaAttribute(int numIterations = 3, int numWarmupIterations = 3)
    {
        NumIterations = numIterations;
    }

    public int NumIterations { get; set; }
    public int NumWarmupIterations { get; set; }
}