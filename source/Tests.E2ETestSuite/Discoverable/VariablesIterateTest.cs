using Sailfish.Attributes;
using Shouldly;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(1, 0)]
public class VariablesIterateTest
{
    [SailfishVariable(1, 2)] public int N { get; set; }

    private string FieldMan = null!;

    public int MyInt { get; set; } = 456;

    [SailfishGlobalSetup]
    public void Setup()
    {
        FieldMan = "WOW";
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        MyInt = 789;
    }

    [SailfishMethod]
    public void Increment()
    {
        FieldMan.ShouldBe("WOW"); 
        Expected.Add(N);
    }

    private static List<int> Expected = new();

    [SailfishMethod]
    public void SecondIncrement()
    {
        MyInt.ShouldBe(789);
        FieldMan.ShouldBe("WOW");
        Expected.Add(N);
    }

    [SailfishGlobalTeardown]
    public void GlobalTeardownAssertions()
    {
        FieldMan.ShouldBe("WOW");
        Expected.ShouldBe(new List<int>() { 1, 2, 1, 2 });
        Expected = new List<int>();
    }
}