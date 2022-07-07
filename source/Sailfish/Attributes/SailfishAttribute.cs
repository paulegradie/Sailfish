using System;

namespace Sailfish.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class SailfishAttribute : Attribute
    {
        internal SailfishAttribute()
        {
        }

        public SailfishAttribute(int numIterations = 3, int numWarmupIterations = 3, bool disabled = false)
        {
            NumIterations = numIterations;
            NumWarmupIterations = numWarmupIterations;
            Disabled = disabled;
        }

        public int NumIterations { get; set; } = 1;
        public int NumWarmupIterations { get; set; } = 1;
        public bool Disabled { get; }
    }
}