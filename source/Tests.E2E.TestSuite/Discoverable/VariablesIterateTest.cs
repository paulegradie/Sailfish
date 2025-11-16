using Sailfish.Attributes;
using Shouldly;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(1, 0, Disabled = Constants.Disabled)]
public class VariablesIterateTest
{
    private static List<int> _expected = new();

    private string _fieldMan = null!;

    [SailfishVariable(1, 2)]
    public int N { get; set; }

    public int MyInt { get; set; } = 456;

    [SailfishGlobalSetup]
    public void Setup()
    {
        _fieldMan = "WOW";
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        MyInt = 789;
    }

    [SailfishMethod]
    public void Increment()
    {
        _fieldMan.ShouldBe("WOW");
        _expected.Add(N);
    }

    [SailfishMethod]
    public void SecondIncrement()
    {
        MyInt.ShouldBe(789);
        _fieldMan.ShouldBe("WOW");
        _expected.Add(N);
    }

    [SailfishGlobalTeardown]
    public void GlobalTeardownAssertions()
    {
        _fieldMan.ShouldBe("WOW");
        _expected.ShouldBe(new List<int> { 1, 2, 1, 2 });
        _expected = new List<int>();
    }
}