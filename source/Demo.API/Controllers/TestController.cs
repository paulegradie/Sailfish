using Microsoft.AspNetCore.Mvc;
using Test.API.Controllers.Base;

namespace Test.API.Controllers
{
    public class TestController : VeerBaseController
    {
        public const string TestResponse = "Hello there!";

        [HttpGet]
        public string Get()
        {
            return TestResponse;
        }
    }
}