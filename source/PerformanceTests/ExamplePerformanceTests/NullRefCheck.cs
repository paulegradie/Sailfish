using Sailfish.Attributes;
using System;
using System.Threading.Tasks;

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
    public async Task OopsNullRef()
    {
        await Task.CompletedTask;
        Console.WriteLine(ThingINeed!.NotPresent);
    }
}