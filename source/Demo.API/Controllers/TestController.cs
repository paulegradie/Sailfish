using Demo.API.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Demo.API.Controllers;

public class TestController : BaseController
{
    public const string TestResponse = "Hello there!";

    [HttpGet]
    public string Get()
    {
        return TestResponse;
    }
}