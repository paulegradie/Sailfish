using System;
using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExceptionHandlingExamples;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class VoidMethodRequestsCancellationToken
{
    [SailfishMethod]
    public void TestMethod(CancellationToken cancellationToken)
    {
        Console.WriteLine("Oops, I forgot to remove the cancellation token");
    }
}