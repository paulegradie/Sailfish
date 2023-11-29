using System.Reflection;
using Sailfish.Logging;

namespace Sailfish.Program;

public static class TestCaseCountPrinter
{
    static int testTypeTotal = 1;
    static int testMethodTotal = 1;
    static int testCaseTotal = 0;

    static int currentType = 1;
    static int currentMethod = 1;
    static int currentTestCase = 1;

    private static ILogger? logger;

    public static void SetTestCaseTotal(int count)
    {
        testCaseTotal = count;
    }

    public static void SetTestMethodTotal(int count)
    {
        testMethodTotal = count;
    }

    public static void SetTestTypeTotal(int count)
    {
        testTypeTotal = count;
    }

    public static void IncrementType()
    {
        currentType += 1;
    }

    public static void IncrementMethod()
    {
        currentMethod += 1;
    }

    public static void IncrementCase()
    {
        currentTestCase += 1;
    }

    public static void PrintDiscoveredTotal()
    {
        if (testCaseTotal > 0)
        {
            logger?.Verbose("Discovered {TotalTestCaseCount} test cases across {TotalTestMethods} test methods from {TotalTestClasses} classes", testCaseTotal, testMethodTotal, testTypeTotal );
        }
    }

    public static void PrintTypeUpdate(string typeName)
    {
        logger?.Verbose("- class {TestIndex} of {TotalTestCount}: {TestName}", currentType, testTypeTotal, typeName);
        IncrementType();
    }

    public static void PrintMethodUpdate(MethodInfo methodInfo)
    {
        var name = $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";
        logger?.Verbose("  -- method {CurrentMethodCount} of {TotalMethodCount}: {Method}", currentMethod, testMethodTotal, name);
        IncrementMethod();
    }

    public static void PrintCaseUpdate(string displayName)
    {
    
        logger?.Verbose("     --- test case {CurrentTestCase} of {TotalTestCases}: {TestCase}", currentTestCase, testCaseTotal, displayName);
        IncrementCase();
    }


    public static void SetLogger(ILogger? l)
    {
        logger = l;
    }
}
