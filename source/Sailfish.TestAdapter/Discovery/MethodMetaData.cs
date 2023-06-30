namespace Sailfish.TestAdapter.Discovery;

public sealed class MethodMetaData
{
    public MethodMetaData(string methodName, int lineNumber)
    {
        MethodName = methodName;
        LineNumber = lineNumber;
    }

    public string MethodName { get; set; }
    public int LineNumber { get; set; }
}