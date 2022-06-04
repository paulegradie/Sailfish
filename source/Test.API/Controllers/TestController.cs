using Microsoft.AspNetCore.Mvc;

namespace Test.API.Controllers;

public class TestController : VeerBaseController
{
    public const string TestResponse = "Hello there!";
    public TestController()
    {
    }

    [HttpGet()]
    public string Get()
    {
        return TestResponse;
    }
}