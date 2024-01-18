using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Library.ApiCommunicationTests.Base;

public class ApiTestBase : IClassFixture<WebApplicationFactory<DemoApp>>, IAsyncDisposable
{
    public ApiTestBase(WebApplicationFactory<DemoApp> factory)
    {
        WebHostFactory = factory.WithWebHostBuilder(
            builder => { builder.UseTestServer(); });
        Client = WebHostFactory.CreateClient();
    }

    public CancellationToken CancellationToken { get; }
    public WebApplicationFactory<DemoApp> WebHostFactory { get; set; }
    public HttpClient Client { get; }

    public async ValueTask DisposeAsync()
    {
        await WebHostFactory.DisposeAsync();
    }
}