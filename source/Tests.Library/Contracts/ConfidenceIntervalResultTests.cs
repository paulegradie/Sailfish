using Shouldly;
using Xunit;
using Sailfish.Contracts.Public.Models;

namespace Tests.Library.Contracts;

public class ConfidenceIntervalResultTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var ci = new ConfidenceIntervalResult(0.95, 1.23, 10.0, 12.46);
        ci.ConfidenceLevel.ShouldBe(0.95);
        ci.MarginOfError.ShouldBe(1.23);
        ci.Lower.ShouldBe(10.0);
        ci.Upper.ShouldBe(12.46);
    }
}

