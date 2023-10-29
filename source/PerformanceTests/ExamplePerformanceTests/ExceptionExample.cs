using System;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(SampleSize = 4, DisableOverheadEstimation = true)]
public class ExceptionExample
{
    [SailfishMethod]
    public void ThrowException()
    {
        throw new Exception();
    }
}