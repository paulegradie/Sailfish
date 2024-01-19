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
        throw new Exception();
    }


    [SailfishMethod]
    public Task DoesNotThrowException(CancellationToken ct)
    {
        // do nothing
        Console.WriteLine(ct.ToString());
        return Task.CompletedTask;
    }
}