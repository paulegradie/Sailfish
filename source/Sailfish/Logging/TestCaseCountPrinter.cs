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
    private readonly ILogger _logger = logger;
    private int _currentMethod = 1;
    private int _currentTestCase = 1;

    private int _currentType = 1;
    private int _testCaseTotal;
    private int _testMethodTotal = 1;
    private int _testTypeTotal = 1;

    public void SetTestCaseTotal(int count)
    {
        _testCaseTotal = count;
    }

    public void SetTestMethodTotal(int count)
    {
        _testMethodTotal = count;
    }

    public void SetTestTypeTotal(int count)
    {
        _testTypeTotal = count;
    }

    public void PrintDiscoveredTotal()
    {
        if (_testCaseTotal > 0)
            WriteUpdate(
                "Discovered {TotalTestCaseCount} test cases across {TotalTestMethods} test methods from {TotalTestClasses} classes", _testCaseTotal, _testMethodTotal,
                _testTypeTotal);
    }

    public void PrintTypeUpdate(string typeName)
    {
        WriteUpdate("- class {TestIndex} of {TotalTestCount}: {TestName}", _currentType, _testTypeTotal, typeName);
        IncrementType();
    }

    public void PrintMethodUpdate(MethodInfo methodInfo)
    {
        var name = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
        WriteUpdate("  -- method {CurrentMethodCount} of {TotalMethodCount}: {Method}", _currentMethod, _testMethodTotal, name);
        IncrementMethod();
    }

    public void PrintCaseUpdate(string displayName)
    {
        WriteUpdate("    --- test case {CurrentTestCase} of {TotalTestCases}: {TestCase}", _currentTestCase, _testCaseTotal, displayName);
        IncrementCase();
    }

    private void IncrementType()
    {
        _currentType += 1;
    }

    private void IncrementMethod()
    {
        _currentMethod += 1;
    }

    private void IncrementCase()
    {
        _currentTestCase += 1;
    }

    private void WriteUpdate(string template, params object[] values)
    {
        _logger.Log(LogLevel.Information, template, values);
    }
}