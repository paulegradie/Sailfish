using Sailfish.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.Utils;

public class SailfishAnalyzerExceptionTests
{
    [Fact]
    public void Ctor_SetsMessage()
    {
        var ex = new SailfishAnalyzerException("oops");
        Assert.IsType<SailfishAnalyzerException>(ex);
        Assert.Equal("oops", ex.Message);
    }
}

