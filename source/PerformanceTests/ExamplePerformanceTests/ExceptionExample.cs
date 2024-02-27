using Sailfish.Attributes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(SampleSize = 4, DisableOverheadEstimation = true)]
public class ExceptionExample
{
    [SailfishMethod]
    public void ThrowException()
    {
        throw new Exception("Where did this go??");
    }

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
        await Task.Yield();
        Console.WriteLine(ct.ToString());
    }

    [SailfishMethod]
    public async ValueTask AlsoDoesNotThrowExceptionWithNoCt()
    {
        await Task.Yield();
    }

    [SailfishMethod]
    public ValueTask FinallyYesDoesNotThrowException()
    {
        return ValueTask.CompletedTask;
    }
}