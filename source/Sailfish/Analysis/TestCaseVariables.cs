﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis;

public class TestCaseVariables
{
    private const string OpenBracket = "(";
    private const string CloseBracket = ")";
    private const char Colon = ':';

    [JsonConstructor]
    public TestCaseVariables()
    {
    }

    public TestCaseVariables(string displayName)
    {
        Variables = GetElements(displayName);
    }

    public TestCaseVariables(IEnumerable<TestCaseVariable> variables)
    {
        Variables = variables.OrderBy(x => x.Name);
    }

    public IEnumerable<TestCaseVariable> Variables { get; set; } = null!;

    public TestCaseVariable? GetVariableIndex(int index)
    {
        try
        {
            return Variables.ToArray()[index];
        }
        catch
        {
            return null;
        }
    }

    public string FormVariableSection()
    {
        var parts = Variables.Select(variable => $"{variable.Name}: {variable.Value.ToString()}").ToList();
        return OpenBracket + string.Join(", ", parts).Trim() + CloseBracket;
    }

    private static TestCaseVariable ParseVariable(string variable)
    {
        // like varName:int
        var parts = variable.Split(Colon);
        return int.TryParse(parts[1], out var value) ? new TestCaseVariable(parts[0], value) : new TestCaseVariable(parts[0], parts[1]);
    }

    private static IEnumerable<TestCaseVariable> GetElements(string s)
    {
        var rawElements = s
            .Split(OpenBracket)
            .Last()
            .Replace(CloseBracket, string.Empty)
            .Split(",")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        if (rawElements.Any())
        {
            return rawElements
                .Select(ParseVariable)
                .OrderByDescending(x => x.Name)
                .ThenBy(x => x.Value)
                .ToList();
        }

        return System.Array.Empty<TestCaseVariable>();
    }
}