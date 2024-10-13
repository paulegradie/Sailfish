using Sailfish.Attributes;
using System;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish]
public class MultipleSailfishVariablesExample
{
    [SailfishVariable(1, 2, 3)]
    public int? VarA { get; set; }

    [SailfishVariable(2, 4, 6)]
    public int? VarB { get; set; }

    [SailfishVariable(2, 4, 6)]
    public int? VarC { get; set; }

    public string? VarD { get; set; }
    public string? VarE { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        VarD = "This is set";
        VarE = "Still Set";
    }

    [SailfishMethod]
    public void MethodA()
    {
        if (VarA is null || VarB is null || VarC is null || VarD is null || VarE is null)
        {
            throw new Exception("There is an error setting class properties");
        }

        Console.WriteLine($"{VarA}-{VarB}-{VarC}-{VarD}-{VarE}");
    }
}