using System;
using System.Linq;
using Sailfish.Analysis;

namespace Sailfish.Utils;

internal static class DisplayNameHelper
{
    public static TestCaseId CreateTestCaseId(Type testType, string methodName, string[] variableNames, int[] paramSet)
    {
        var nameParts = new[] { testType.Name, methodName };
        var testCaseName = new TestCaseName(nameParts);

        if (variableNames.Length != paramSet.Length) throw new Exception("Number of variables and number of params does not match");
        var namedParams = variableNames.Zip(paramSet);

        var variables = namedParams.OrderBy(x => x.First).Select(x => new TestCaseVariable(x.First, x.Second));
        var testCaseVariables = new TestCaseVariables(variables);
        return new TestCaseId(testCaseName, testCaseVariables);
    }

    public static string CreateParamsDisplay(string[] variableNames, int[] paramSet)
    {
        if (variableNames.Length != paramSet.Length) throw new Exception("Number of variables and number of params does not match");
        var namedParams = variableNames.Zip(paramSet);

        return "(" + string.Join(", ", namedParams.Select(FormNameString).ToArray()) + ")";
    }

    private static string FormNameString((string First, int Second) pair)
    {
        return $"{pair.First}: {pair.Second}";
    }
}