using System.Text.Json.Serialization;

namespace Sailfish.Analysis;

public class TestCaseVariable
{
    [JsonConstructor]
    public TestCaseVariable()
    {
    }

    public TestCaseVariable(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; } = null!;
    public int Value { get; set; }
}