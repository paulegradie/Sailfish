using System.Threading.Tasks;
using Demo.API;
using Demo.API.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Tests.Library.ApiCommunicationTests.Base;
using Xunit;

namespace Tests.Library.ApiCommunicationTests;

public class WhenTalkingToTheApi : ApiTestBase
{
    public WhenTalkingToTheApi(WebApplicationFactory<DemoApp> factory) : base(factory)
    {
    }

    [Fact]
    public async Task AResponseIsReturned()
    {
        var response = await Client.GetStringAsync("/");
        response.ShouldBe(TestController.TestResponse);
    }

    [Fact]
    public async Task AMillionIsCounted()
    {
        var response = await Client.GetStringAsync("/million");
        response.ShouldBe("1000000");
    }
}