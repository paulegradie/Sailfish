using System;

namespace VeerPerforma.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ExecutePerformanceCheckAttribute : Attribute
    {
    }
}