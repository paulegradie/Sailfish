using System.Threading;
using Sailfish.Attributes;
using Shouldly;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(3, 0)]
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
        Thread.Sleep(20 * N);
        FieldMan.ShouldBe("WOW");
    }

    [SailfishMethod]
    public void SecondIncrement()
    {
        Thread.Sleep(10 * N);
        FieldMan.ShouldBe("WOW");
    }
}