using System.Linq;
using Sailfish.Analysis;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis;

public class ConfigurableOutlierDetectorTests
{
    private readonly ConfigurableOutlierDetector detector = new();

    [Fact]
    public void RemoveUpper_WithUpperOutliers_RemovesOnlyUpper()
    {
        var data = new[] { 1.0, 2.0, 3.0, 100.0 };
        var result = detector.DetectOutliers(data, OutlierStrategy.RemoveUpper);

        result.UpperOutliers.ShouldContain(100.0);
        result.LowerOutliers.ShouldBeEmpty();
        result.DataWithOutliersRemoved.ShouldContain(1.0);
        result.DataWithOutliersRemoved.ShouldContain(2.0);
        result.DataWithOutliersRemoved.ShouldContain(3.0);
        result.DataWithOutliersRemoved.ShouldNotContain(100.0);
    }

    [Fact]
    public void RemoveLower_WithLowerOutliers_RemovesOnlyLower()
    {
        var data = new[] { -100.0, 1.0, 2.0, 3.0 };
        var result = detector.DetectOutliers(data, OutlierStrategy.RemoveLower);

        result.LowerOutliers.ShouldContain(-100.0);
        result.UpperOutliers.ShouldBeEmpty();
        result.DataWithOutliersRemoved.ShouldContain(1.0);
        result.DataWithOutliersRemoved.ShouldContain(2.0);
        result.DataWithOutliersRemoved.ShouldContain(3.0);
        result.DataWithOutliersRemoved.ShouldNotContain(-100.0);
    }

    [Fact]
    public void RemoveAll_RemovesBothSides()
    {
        var data = new[] { -100.0, 1.0, 2.0, 3.0, 100.0 };
        var result = detector.DetectOutliers(data, OutlierStrategy.RemoveAll);

        result.LowerOutliers.ShouldContain(-100.0);
        result.UpperOutliers.ShouldContain(100.0);
        result.DataWithOutliersRemoved.ShouldContain(1.0);
        result.DataWithOutliersRemoved.ShouldContain(2.0);
        result.DataWithOutliersRemoved.ShouldContain(3.0);
        result.DataWithOutliersRemoved.ShouldNotContain(-100.0);
        result.DataWithOutliersRemoved.ShouldNotContain(100.0);
    }

    [Fact]
    public void DontRemove_ReportsButDoesNotFilter()
    {
        var data = new[] { -100.0, 1.0, 2.0, 3.0, 100.0 };
        var result = detector.DetectOutliers(data, OutlierStrategy.DontRemove);

        result.OriginalData.Length.ShouldBe(data.Length);
        result.DataWithOutliersRemoved.Length.ShouldBe(data.Length);
        (result.LowerOutliers.Count() + result.UpperOutliers.Count()).ShouldBe(result.TotalNumOutliers);
    }

    [Fact]
    public void Adaptive_ChoosesSideBasedOnAsymmetry()
    {
        var onlyUpper = new[] { 10.0, 11.0, 12.0, 200.0 };
        var r1 = detector.DetectOutliers(onlyUpper, OutlierStrategy.Adaptive);
        r1.UpperOutliers.Any().ShouldBeTrue();
        r1.LowerOutliers.Any().ShouldBeFalse();
        r1.DataWithOutliersRemoved.ShouldNotContain(200.0);

        var onlyLower = new[] { -200.0, 10.0, 11.0, 12.0 };
        var r2 = detector.DetectOutliers(onlyLower, OutlierStrategy.Adaptive);
        r2.LowerOutliers.Any().ShouldBeTrue();
        r2.UpperOutliers.Any().ShouldBeFalse();
        r2.DataWithOutliersRemoved.ShouldNotContain(-200.0);

        var both = new[] { -200.0, 10.0, 11.0, 12.0, 200.0 };
        var r3 = detector.DetectOutliers(both, OutlierStrategy.Adaptive);
        r3.DataWithOutliersRemoved.ShouldContain(10.0);
        r3.DataWithOutliersRemoved.ShouldContain(11.0);
        r3.DataWithOutliersRemoved.ShouldContain(12.0);
        r3.DataWithOutliersRemoved.ShouldNotContain(-200.0);
        r3.DataWithOutliersRemoved.ShouldNotContain(200.0);
    }

    [Fact]
    public void SmallSamples_DoNotRemove()
    {
        var tiny = new[] { 1.0, 100.0, 2.0 };
        var result = detector.DetectOutliers(tiny, OutlierStrategy.RemoveAll);
        result.DataWithOutliersRemoved.Length.ShouldBe(3);
        result.TotalNumOutliers.ShouldBe(0);
    }
}

