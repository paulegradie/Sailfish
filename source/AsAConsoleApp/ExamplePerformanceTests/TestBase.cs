using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Test.API;
using Xunit;

namespace AsAConsoleApp.ExamplePerformanceTests;

public class TestBase : IClassFixture<WebApplicationFactory<DemoApp>>, IAsyncDisposable
{
    public TestBase(WebApplicationFactory<DemoApp> factory)
    {
        WebHostFactory = factory.WithWebHostBuilder(
            builder => { builder.UseTestServer(); });
        Client = WebHostFactory.CreateClient();
    }

    public WebApplicationFactory<DemoApp> WebHostFactory { get; set; }
    public HttpClient Client { get; }

    public async ValueTask DisposeAsync()
    {
        await WebHostFactory.DisposeAsync();
    }
}