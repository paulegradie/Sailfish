using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Models;

public class TestCaseVariable
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TestCaseVariable()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public TestCaseVariable(string name, object value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; } = null!;
    public object Value { get; set; }
}