using System;
using Sailfish.Attributes;

namespace PerformanceTests.ExceptionHandlingExamples;

[Sailfish]
public class WillThrow
{
    [SailfishMethod]
    public void ThisThrows()
    {
        throw new Exception("This was my exception!");
    }
}