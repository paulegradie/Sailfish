using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Sailfish.Execution;
using Test.API;
using Xunit;

namespace AsAConsoleApp.ExamplePerformanceTests
{
    public class TestBase : IClassFixture<WebApplicationFactory<DemoApp>>, IAsyncDisposable
    {
        public TestBase(WebApplicationFactory<DemoApp> factory, CancellationTokenAccess ctAccess)
        {
            CancellationToken = ctAccess.Token ?? CancellationToken.None;
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
}