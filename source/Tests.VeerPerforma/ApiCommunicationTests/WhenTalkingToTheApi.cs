using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Test.API;
using Test.API.Controllers;
using Test.ApiCommunicationTests.Base;
using Xunit;

namespace Test.ApiCommunicationTests
{
    public class WhenTalkingToTheApi : ApiTestBase
    {
        public WhenTalkingToTheApi(WebApplicationFactory<MyApp> factory) : base(factory)
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
}