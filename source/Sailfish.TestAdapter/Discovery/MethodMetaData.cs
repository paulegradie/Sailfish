namespace Sailfish.TestAdapter.Discovery;

public sealed class MethodMetaData
{
    public MethodMetaData(string methodName, int lineNumber)
    {
        MethodName = methodName;
        LineNumber = lineNumber;
    }

    public MethodMetaData(string methodName, int lineNumber, string? comparisonGroup)
    {
        MethodName = methodName;
        LineNumber = lineNumber;
        ComparisonGroup = comparisonGroup;
    }

    public string MethodName { get; set; }
    public int LineNumber { get; set; }

    /// <summary>
    /// The comparison group this method belongs to, if any.
    /// Methods with the same comparison group will be compared against each other.
    /// </summary>
    public string? ComparisonGroup { get; set; }
}