namespace VeerPerforma;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class VeerPerformaAttribute : Attribute
{
    internal VeerPerformaAttribute()
    {
    }

    public VeerPerformaAttribute(int numIterations = 3)
    {
        NumIterations = numIterations;
    }

    public int NumIterations { get; set; }
}

/// <summary>
/// This is used to decorate a property that will be referenced within the test.
/// A unique execution set of the performance tests is executed for each value provided,
/// where an execution set is the total number of executions specified by the
/// VeerPerforma attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class IterationVariable : Attribute
{
    public IterationVariable(params int[] n)
    {
        N = n;
    }

    public int[] N { get; set; }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ExecutePerformanceCheck : Attribute
{
}