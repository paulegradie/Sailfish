using System;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

public abstract class NullRefOnTheBase
{
    public NullThing? ThingINeed { get; set; }

    public abstract record NullThing
    {
        protected NullThing(object NotPresent)
        {
            this.NotPresent = NotPresent;
        }

        public object NotPresent { get; init; }

        public void Deconstruct(out object NotPresent)
        {
            NotPresent = this.NotPresent;
        }
    }
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