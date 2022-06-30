using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Test.API;
using Xunit;

namespace Test.ApiCommunicationTests.Base
{
    public class ApiTestBase : IClassFixture<WebApplicationFactory<DemoApp>>
    {
        public ApiTestBase(WebApplicationFactory<DemoApp> factory)
        {
            WebHostFactory = factory.WithWebHostBuilder(
                builder => { builder.UseTestServer(); });
            Client = WebHostFactory.CreateClient();
        }

        public WebApplicationFactory<DemoApp> WebHostFactory { get; set; }
        public HttpClient Client { get; }
    }
}