using Sailfish.Analysis;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis;

public class TDistributionTableTests
{
    [Theory]
    [InlineData(1, 0.95, 12.706, 1e-3)]
    [InlineData(5, 0.95, 2.571, 1e-3)]
    [InlineData(10, 0.95, 2.228, 1e-3)]
    [InlineData(10, 0.99, 3.169, 1e-3)]
    public void CriticalValues_ShouldMatchKnownGood(int df, double cl, double expected, double tol)
    {
        var t = TDistributionTable.GetCriticalValue(cl, df);
        t.ShouldBe(expected, tol);
    }

    [Fact]
    public void CriticalValue_ShouldDecreaseWithDf()
    {
        var t5 = TDistributionTable.GetCriticalValue(0.95, 5);
        var t30 = TDistributionTable.GetCriticalValue(0.95, 30);
        t30.ShouldBeLessThan(t5);
  
    }
    [Theory]
    [InlineData(0, 0.95)]
    [InlineData(-5, 0.95)]
    [InlineData(1, 0.0)]
    [InlineData(1, 1.0)]
    public void InvalidInputs_AreCoerced_ToSafeDefaults(int df, double cl)
    {
        // Should not throw and should return a finite value
        var t = TDistributionTable.GetCriticalValue(cl, df);
        t.ShouldBeGreaterThan(0);
        t.ShouldBeLessThan(100);
    }

}


