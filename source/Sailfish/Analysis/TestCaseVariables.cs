using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis;

public class TestCaseVariables
{
    private const char OpenBracket = '(';
    private const char CloseBracket = ')';
    private const char Dot = '.';
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
        Variables = variables;
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
        return OpenBracket + string.Join(", ", parts) + CloseBracket;
    }

    private static TestCaseVariable ParseVariable(string variable)
    {
        // like varName:int
        var parts = variable.Split(Colon);
        return new TestCaseVariable(parts[0], int.Parse(parts[1]));
    }

    private static IEnumerable<TestCaseVariable> GetElements(string s)
    {
        var elements = s
            .Split(OpenBracket)
            .Last()
            .Replace(")", string.Empty)
            .Split(",")
            .Select(ParseVariable)
            .OrderBy(x => x.Name)
            .ToList();
        return elements;
    }
}