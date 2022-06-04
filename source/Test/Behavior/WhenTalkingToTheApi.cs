using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Test.API.Controllers;
using Test.Behavior.Base;
using Xunit;

namespace Test.Behavior;

public class WhenTalkingToTheApi : ApiTestBase
{
    [Fact]
    public async Task AResponseIsReturned()
    {
        var response = await Client.GetStringAsync("/");
        response.ShouldBe(TestController.TestResponse);
    }

    public WhenTalkingToTheApi(WebApplicationFactory<MyApp> factory) : base(factory)
    {
    }
}