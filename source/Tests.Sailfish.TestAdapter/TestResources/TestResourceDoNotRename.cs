using System;
using System.IO;
using Sailfish.Attributes;

namespace Tests.Sailfish.TestAdapter.TestResources;

[Sailfish( Disabled = true)]
public class SimplePerfTest
{
    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishVariable(1_000_000, 4_000_000)]
    public int VariableB { get; set; }

    [SailfishMethod]
    public void ExecutionMethod()
    {
        for (var i = 0; i < 100; i++) Console.SetOut(new StreamWriter(Stream.Null));
    }
}