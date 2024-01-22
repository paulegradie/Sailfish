using System.Reflection;

namespace Sailfish.Logging;

internal interface ITestCaseCountPrinter
{
    void SetTestCaseTotal(int count);

    void SetTestMethodTotal(int count);

    void SetTestTypeTotal(int count);

    void PrintDiscoveredTotal();

    void PrintTypeUpdate(string typeName);

    void PrintMethodUpdate(MethodInfo methodInfo);

    void PrintCaseUpdate(string displayName);
}

internal class TestCaseCountPrinter(ILogger logger) : ITestCaseCountPrinter
{
    private readonly ILogger logger = logger;
    private int currentMethod = 1;
    private int currentTestCase = 1;

    private int currentType = 1;
    private int testCaseTotal;
    private int testMethodTotal = 1;
    private int testTypeTotal = 1;

    public void SetTestCaseTotal(int count)
    {
        testCaseTotal = count;
    }

    public void SetTestMethodTotal(int count)
    {
        testMethodTotal = count;
    }

    public void SetTestTypeTotal(int count)
    {
        testTypeTotal = count;
    }

    public void PrintDiscoveredTotal()
    {
        if (testCaseTotal > 0)
            WriteUpdate(
                "Discovered {TotalTestCaseCount} test cases across {TotalTestMethods} test methods from {TotalTestClasses} classes", testCaseTotal, testMethodTotal,
                testTypeTotal);
    }

    public void PrintTypeUpdate(string typeName)
    {
        WriteUpdate("- class {TestIndex} of {TotalTestCount}: {TestName}", currentType, testTypeTotal, typeName);
        IncrementType();
    }

    public void PrintMethodUpdate(MethodInfo methodInfo)
    {
        var name = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
        WriteUpdate("  -- method {CurrentMethodCount} of {TotalMethodCount}: {Method}", currentMethod, testMethodTotal, name);
        IncrementMethod();
    }

    public void PrintCaseUpdate(string displayName)
    {
        WriteUpdate("    --- test case {CurrentTestCase} of {TotalTestCases}: {TestCase}", currentTestCase, testCaseTotal, displayName);
        IncrementCase();
    }

    private void IncrementType()
    {
        currentType += 1;
    }

    private void IncrementMethod()
    {
        currentMethod += 1;
    }

    private void IncrementCase()
    {
        currentTestCase += 1;
    }

    private void WriteUpdate(string template, params object[] values)
    {
        logger.Log(LogLevel.Information, template, values);
    }
}