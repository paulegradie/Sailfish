using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Sailfish.Attributes;
using Sailfish.Registration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.TestSuite.Utils;

public class TestBase : ISailfishFixture<SailfishDependencies>
{
    protected TestBase(WebApplicationFactory<DemoApp> factory)
    {
        WebHostFactory = factory.WithWebHostBuilder(
            builder => { builder.UseTestServer(); });
        Client = WebHostFactory.CreateClient();
    }

    public WebApplicationFactory<DemoApp> WebHostFactory { get; set; }
    protected HttpClient Client { get; }

    [SailfishGlobalTeardown]
    public virtual async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Client.GetAsync("api", cancellationToken);
    }
}