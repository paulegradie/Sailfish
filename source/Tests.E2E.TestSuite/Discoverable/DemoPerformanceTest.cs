using Sailfish.Attributes;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(Disabled = Constants.Disabled)]
public class DemoPerformanceTest
{
    [SailfishVariable(1, 4, 6)]
    public int MyInts { get; set; }

    [SailfishVariable(2, 3, 7)]
    public int MyIntsTwo { get; set; }

    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(13, cancellationToken);
    }
}