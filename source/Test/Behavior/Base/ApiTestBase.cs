using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Test.Behavior.Base;

public class ApiTestBase : IClassFixture<WebApplicationFactory<MyApp>>
{
    public WebApplicationFactory<MyApp> WebHostFactory { get; set; }
    public HttpClient Client { get; }

    public ApiTestBase(WebApplicationFactory<MyApp> factory)
    {
        WebHostFactory = factory.WithWebHostBuilder(
            builder => { builder.UseTestServer(); });
        Client = WebHostFactory.CreateClient();
    }
}