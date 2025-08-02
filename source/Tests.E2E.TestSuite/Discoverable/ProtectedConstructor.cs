using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Attributes;
using System.Threading;
using System.Threading.Tasks;
using Tests.E2E.TestSuite.Utils;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
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