using Sailfish.Attributes;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = false)]
public class TestsRunIfException
{
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