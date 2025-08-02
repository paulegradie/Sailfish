using Sailfish.Attributes;
using System;
using System.Threading;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = false, DisableOverheadEstimation = true)]
public class SailfishMethodException
{
    [SailfishMethod]
    public void MainMethod()
    {
        throw new Exception();
    }

    [SailfishMethod]
    public void OtherMethod()
    {
        Thread.Sleep(100);
    }
}