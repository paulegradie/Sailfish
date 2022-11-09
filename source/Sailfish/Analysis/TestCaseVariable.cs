namespace Sailfish.Analysis;

public class TestCaseVariable
{
    public TestCaseVariable(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    public int Value { get; set; }
}