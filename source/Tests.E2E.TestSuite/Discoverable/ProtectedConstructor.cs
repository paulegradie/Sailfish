using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Attributes;
using Tests.E2ETestSuite.Utils;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(NumSamples = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
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