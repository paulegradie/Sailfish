using Sailfish.Attributes;
using System;
using System.Threading.Tasks;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish]
public class TestsRunIfException
{
    [SailfishMethodSetup]
    public async Task MethodSetup()
    {
        await Task.Yield();
    }
    
    [SailfishMethod(Order = 1)]
    public async Task WillItRun()
    {
        await Task.Yield();
        Console.WriteLine("It Ran");
    }

    [SailfishMethod(Order = 2)]
    public async Task ThisWillThrow()
    {
        await Task.Yield();
        throw new TestException("OH No");
    }

    [SailfishMethod(Order = 3)]
    public async Task ItHadBetterRun()
    {
        await Task.Yield();
        Console.WriteLine("It also ran!");
    }
}
