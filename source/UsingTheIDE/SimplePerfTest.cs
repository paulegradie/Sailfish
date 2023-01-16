using Sailfish.Attributes;

namespace UsingTheIDE;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2)]
public class SimplePerfTest : SailfishBase
{
    private readonly ExampleDep exampleDep;

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishVariable(100, 400)] public int VariableB { get; set; }

    [SailfishMethod]
    public void Go()
    {
        for (var i = 0; i < VariableA; i++)
        {
            for (var j = 0; j < VariableB; j++)
            {
                exampleDep.WriteSomething("Wow");
            }
        }
    }

    public SimplePerfTest(ExampleDep exampleDep, SailfishDependencies sailfishDependencies) : base(sailfishDependencies)
    {
        this.exampleDep = exampleDep;
    }
}