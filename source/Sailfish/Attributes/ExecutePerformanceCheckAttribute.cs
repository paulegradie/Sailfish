using System;

namespace Sailfish.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ExecutePerformanceCheckAttribute : Attribute
    {
    }
}