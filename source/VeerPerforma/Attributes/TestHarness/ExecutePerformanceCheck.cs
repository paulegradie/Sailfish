using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VeerPerforma.Attributes.TestHarness;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ExecutePerformanceCheckAttribute : TestMethodAttribute
{
}