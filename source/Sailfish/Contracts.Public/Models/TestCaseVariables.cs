using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Models;

public class TestCaseVariables
{
    private const string OpenBracket = "(";
    private const string CloseBracket = ")";
    private const char Colon = ':';

#pragma warning disable CS8618
    public TestCaseVariables()
#pragma warning restore CS8618
    {
        
    }
    
    public TestCaseVariables(string displayName)
    {
        Variables = GetElements(displayName);
    }

    [JsonConstructor]
    public TestCaseVariables(IEnumerable<TestCaseVariable> variables)
    {
        Variables = variables.OrderBy(x => x.Name);
    }

    public IEnumerable<TestCaseVariable> Variables { get; }

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

    public TestCaseVariable GetVariableByName(string name)
    {
        return Variables.Single(x => x.Name.Equals(name));
    }

    public string FormVariableSection()
    {
        var parts = Variables.Select(variable => $"{variable.Name.Trim()}: {variable.Value.ToString()?.Trim() ?? string.Empty}");
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