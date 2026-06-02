namespace Sailfish.TestAdapter.Discovery;

public sealed class MethodMetaData
{
    public MethodMetaData(string methodName, int lineNumber)
    {
        MethodName = methodName;
        LineNumber = lineNumber;
    }

    public MethodMetaData(string methodName, int lineNumber, string? comparisonGroup)
        : this(methodName, lineNumber, comparisonGroup, isBaseline: false)
    {
    }

    public MethodMetaData(string methodName, int lineNumber, string? comparisonGroup, bool isBaseline)
    {
        MethodName = methodName;
        LineNumber = lineNumber;
        ComparisonGroup = comparisonGroup;
        IsBaseline = isBaseline;
    }

    public string MethodName { get; set; }
    public int LineNumber { get; set; }

    /// <summary>
    /// The comparison group this method belongs to, if any.
    /// Methods with the same comparison group will be compared against each other.
    /// </summary>
    public string? ComparisonGroup { get; set; }

    /// <summary>
    /// True when this method is the comparison group's baseline
    /// (<c>[SailfishMethod(IsBaseline = true)]</c>). Every other method in the group is reported
    /// relative to it.
    /// </summary>
    public bool IsBaseline { get; set; }
}