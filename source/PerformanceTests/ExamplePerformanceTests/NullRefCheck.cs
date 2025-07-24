using System;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

public abstract class NullRefOnTheBase
{
    public NullThing? ThingINeed { get; set; }

    public abstract record NullThing(object NotPresent);
}

[Sailfish]
public class NullRefCheck : NullRefOnTheBase
{
    [SailfishMethod]
    public async Task OopsNullRefFails()
    {
        await Task.CompletedTask;
        Console.WriteLine(ThingINeed!.NotPresent);
    }
}