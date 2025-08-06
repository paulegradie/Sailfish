using System;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;

namespace Sailfish.Utils;

internal static class DisplayNameHelper
{
    public static TestCaseId CreateTestCaseId(Type testType, string methodName, string[] variableNames, object[] paramSet)
    {
        var nameParts = new[] { testType.Name, methodName };
        var testCaseName = new TestCaseName(nameParts);

        if (variableNames.Length != paramSet.Length) throw new SailfishException("Number of variables and number of params does not match");
        var namedParams = variableNames.Zip(paramSet);

        var variables = namedParams.OrderBy(x => x.First).Select(x => new TestCaseVariable(x.First, x.Second));
        var testCaseVariables = new TestCaseVariables(variables);
        return new TestCaseId(testCaseName, testCaseVariables);
    }

    public static string FullyQualifiedName(Type type, string methodName)
    {
        // Handle case where there might be multiple overloaded methods
        var methods = type.GetMethods().Where(m => m.Name == methodName).ToArray();
        if (methods.Length == 0)
            throw new SailfishException($"Method name: {methodName} was not found on type {type.Name}");

        // Use the first method if there are multiple overloads
        var methodInfo = methods.First();

        var names = $"{type.Namespace}.{type.Name}.{methodName}";

        var parameters = methodInfo.GetParameters();
        if (parameters.Length > 0)
        {
            var ps = parameters.Select(p => p.ParameterType.Name);
            var parametersJoined = string.Join(", ", ps);

            names += $"({parametersJoined})";
        }
        else
        {
            names += "()";
        }

        return names;
    }
}