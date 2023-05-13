using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Sailfish.Registration;

namespace Tests.E2ETestSuite.Utils;

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

    public virtual async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Client.GetAsync("api", cancellationToken);
    }
}