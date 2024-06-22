using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Exceptions;

namespace Sailfish.Contracts.Public.Models;

public class TestCaseIdComparer : IComparer<TestCaseId>
{
    public int Compare(TestCaseId? x, TestCaseId? y)
    {
        if (x is null || y is null) throw new SailfishException("TestCaseIds shouldn't be null");

        var nameComparison = string.Compare(x.TestCaseName.Name, y.TestCaseName.Name, StringComparison.InvariantCultureIgnoreCase);
        return nameComparison != 0
            ? nameComparison
            : CompareTestCaseVariables(x.TestCaseVariables, y.TestCaseVariables);
    }

    private static int CompareTestCaseVariables(TestCaseVariables xVars, TestCaseVariables yVars)
    {
        var xList = xVars.Variables.ToList();
        var yList = yVars.Variables.ToList();

        // Compare each element of the TestCaseVariables
        for (var i = 0; i < Math.Max(xList.Count, yList.Count); i++)
        {
            var xVar = xVars.GetVariableIndex(i);
            var yVar = yVars.GetVariableIndex(i);

            // If x has more variables, it comes after y
            if (xVar == null)
                return -1;

            // If y has more variables, it comes after x
            if (yVar == null)
                return 1;

            // Compare based on variable name
            var nameComparison = string.Compare(xVar.Name, yVar.Name, StringComparison.InvariantCultureIgnoreCase);
            if (nameComparison != 0)
                return nameComparison;

            // If names are equal, compare based on variable value (if numeric)
            if (xVar.Value is int xIntValue && yVar.Value is int yIntValue)
            {
                var valueComparison = xIntValue.CompareTo(yIntValue);
                if (valueComparison != 0)
                    return valueComparison;
            }

            // If names are equal and values are non-numeric, compare based on string representation
            var stringValueComparison = string.Compare(xVar.Value.ToString(), yVar.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);
            if (stringValueComparison != 0)
                return stringValueComparison;
        }

        // If everything is equal, the elements are considered equal
        return 0;
    }
}