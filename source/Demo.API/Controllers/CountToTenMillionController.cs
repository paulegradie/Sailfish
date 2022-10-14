using Microsoft.AspNetCore.Mvc;
using Test.API.Controllers.Base;

namespace Test.API.Controllers;

public class CountToTenMillionController : BaseController
{
    public const string Route = "million";

    [HttpGet(Route)]
    public string CountToAMillion()
    {
        var start = 0;
        do
        {
            start++;
        } while (start < 1_000_000);

        return start.ToString();
    }
}