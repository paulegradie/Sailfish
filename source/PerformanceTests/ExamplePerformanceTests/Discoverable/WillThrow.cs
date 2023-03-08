using System;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish]
public class WillThrow
{
    [SailfishMethod]
    public void ThisThrows()
    {
        throw new Exception("This was my exception!");
    }
}