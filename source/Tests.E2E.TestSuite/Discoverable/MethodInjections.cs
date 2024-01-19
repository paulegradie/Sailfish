using Sailfish.Attributes;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
internal class MethodInjections
{
    [SailfishMethod]
    public Task DoesNotThrowException(CancellationToken ct)
    {
        Console.WriteLine(ct.ToString());
        return Task.CompletedTask;
    }

    [SailfishMethod]
    public void AlsoDoesNotThrowException(CancellationToken ct)
    {
        Console.WriteLine(ct.ToString());
    }

    [SailfishMethod]
    public ValueTask YesAlsoDoesNotThrowException(CancellationToken ct)
    {
        Console.WriteLine(ct.ToString());
        return ValueTask.CompletedTask;
    }

    [SailfishMethod]
    public async ValueTask FinallyDoesNotThrowException(CancellationToken ct)
    {
        await ValueTask.CompletedTask;
    }

    [SailfishMethod]
    public async ValueTask FinallyDoesNotThrowException()
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    [SailfishMethod]
    public ValueTask FinallyYesDoesNotThrowException()
    {
        return ValueTask.CompletedTask;
    }
}
