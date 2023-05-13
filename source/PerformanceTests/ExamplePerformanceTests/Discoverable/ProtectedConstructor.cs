using System.Threading;
using System.Threading.Tasks;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using PerformanceTests.DemoUtils;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class ProtectedConstructor : TestBase
{
    protected ProtectedConstructor(WebApplicationFactory<DemoApp> factory) : base(factory)
    {
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken)
    {
        await Task.Delay(15, cancellationToken);
    }
}