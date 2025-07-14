using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
public class StringVariablesFromMethodTest
{
    [SailfishVariable(typeof(StringVariablesProvider))]
    public string MyString { get; set; }

    public static AsyncLocal<List<string>> StringVariables = new();
    [SailfishMethod]
    public void Run()
    {
        if (StringVariables.Value is not null)
        {
            StringVariables.Value.Add(MyString);
        }
    }
}

public class StringVariablesProvider : ISailfishVariablesProvider<string>
{
    public IEnumerable<string> Variables()
    {
        return ["Z", "A", "B"];
    }
}