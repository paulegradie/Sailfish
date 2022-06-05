using Microsoft.AspNetCore.Mvc;
using Test.API.Controllers.Base;

namespace Test.API.Controllers;

public class CountToTenMillionController : VeerBaseController
{
    [HttpGet("million")]
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