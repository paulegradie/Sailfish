namespace Sailfish.TestAdapter.Discovery;

public sealed class MethodMetaData
{
    public MethodMetaData(string methodName, int lineNumber, string? comparisonGroup = null, string? baselineMethod = null, double significanceLevel = 0.05)
    {
        MethodName = methodName;
        LineNumber = lineNumber;
        ComparisonGroup = comparisonGroup;
        BaselineMethod = baselineMethod;
        SignificanceLevel = significanceLevel;
    }

    public string MethodName { get; set; }
    public int LineNumber { get; set; }
    public string? ComparisonGroup { get; set; }
    public string? BaselineMethod { get; set; }
    public double SignificanceLevel { get; set; }

    public bool IsPartOfComparison => !string.IsNullOrEmpty(ComparisonGroup);
}