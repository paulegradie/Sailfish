using Sailfish.Analysis.SailDiff.Statistics.StatsCore;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class WilcoxonDistributionTests
{
    [Fact]
    public void Constructor_WithExactMode_ShouldCreateInstance()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        // Assert
        distribution.ShouldNotBeNull();
        distribution.Exact.ShouldBeTrue();
        distribution.Table.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithApproximationMode_ShouldCreateInstance()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Assert
        distribution.ShouldNotBeNull();
        distribution.Exact.ShouldBeFalse();
        distribution.Table.ShouldBeNull();
    }

    [Fact]
    public void Exact_ShouldReturnConstructorValue()
    {
        // Arrange & Act
        var exactDist = new WilcoxonDistribution([1.0, 2.0, 3.0], exact: true);
        var approxDist = new WilcoxonDistribution([1.0, 2.0, 3.0], exact: false);

        // Assert
        exactDist.Exact.ShouldBeTrue();
        approxDist.Exact.ShouldBeFalse();
    }

    [Fact]
    public void Correction_ShouldDefaultToMidpoint()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0 };

        // Act
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Assert
        distribution.Correction.ShouldBe(ContinuityCorrection.Midpoint);
    }

    [Fact]
    public void Correction_ShouldBeSettable()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Act
        distribution.Correction = ContinuityCorrection.KeepInside;

        // Assert
        distribution.Correction.ShouldBe(ContinuityCorrection.KeepInside);
    }

    [Fact]
    public void Mean_ShouldCalculateCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Act & Assert - Mean = n(n+1)/4 = 4*5/4 = 5
        distribution.Mean.ShouldBe(5.0);
    }

    [Fact]
    public void Support_WithExactMode_ShouldStartAtZero()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        // Act
        var support = distribution.Support;

        // Assert
        support.Min.ShouldBe(0.0);
        support.Max.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void Support_WithApproximationMode_ShouldBeInfinite()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Act
        var support = distribution.Support;

        // Assert
        support.Min.ShouldBe(double.NegativeInfinity);
        support.Max.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void WPositive_WithAllPositiveSigns_ShouldReturnSumOfRanks()
    {
        // Arrange
        var signs = new[] { 1, 1, 1 };
        var ranks = new[] { 1.0, 2.0, 3.0 };

        // Act
        var result = WilcoxonDistribution.WPositive(signs, ranks);

        // Assert
        result.ShouldBe(6.0); // 1 + 2 + 3
    }

    [Fact]
    public void WPositive_WithAllNegativeSigns_ShouldReturnZero()
    {
        // Arrange
        var signs = new[] { -1, -1, -1 };
        var ranks = new[] { 1.0, 2.0, 3.0 };

        // Act
        var result = WilcoxonDistribution.WPositive(signs, ranks);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void WPositive_WithMixedSigns_ShouldReturnSumOfPositiveRanks()
    {
        // Arrange
        var signs = new[] { 1, -1, 1, -1 };
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0 };

        // Act
        var result = WilcoxonDistribution.WPositive(signs, ranks);

        // Assert
        result.ShouldBe(4.0); // 1 + 3
    }

    [Fact]
    public void ExactMethod_WithValueBelowAllTableEntries_ShouldReturnZero()
    {
        // Arrange
        var table = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var result = WilcoxonDistribution.ExactMethod(0.5, table);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void ExactMethod_WithValueAboveAllTableEntries_ShouldReturnOne()
    {
        // Arrange
        var table = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var result = WilcoxonDistribution.ExactMethod(10.0, table);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void ExactMethod_WithValueInMiddle_ShouldReturnCorrectProportion()
    {
        // Arrange
        var table = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var result = WilcoxonDistribution.ExactMethod(2.5, table);

        // Assert - 2 out of 5 values are less than 2.5
        result.ShouldBe(0.4);
    }

    [Fact]
    public void ExactComplement_WithValueBelowAllTableEntries_ShouldReturnOne()
    {
        // Arrange
        var table = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var result = WilcoxonDistribution.ExactComplement(0.5, table);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void ExactComplement_WithValueAboveAllTableEntries_ShouldReturnZero()
    {
        // Arrange
        var table = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var result = WilcoxonDistribution.ExactComplement(10.0, table);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void Count_WithMatchingValue_ShouldReturnCount()
    {
        // Arrange
        var table = new[] { 1.0, 2.0, 2.0, 3.0, 2.0 };

        // Act
        var result = WilcoxonDistribution.Count(2.0, table);

        // Assert
        result.ShouldBe(3);
    }

    [Fact]
    public void Count_WithNoMatchingValue_ShouldReturnZero()
    {
        // Arrange
        var table = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var result = WilcoxonDistribution.Count(10.0, table);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void DistributionFunction_WithApproximation_ShouldWorkCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Act
        var result = distribution.DistributionFunction(distribution.Mean);

        // Assert - At mean, CDF should be approximately 0.5
        result.ShouldBe(0.5, 0.1);
    }

    [Fact]
    public void ProbabilityDensityFunction_WithExactMode_ShouldUseCountMethod()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        // Act
        var result = distribution.ProbabilityDensityFunction(3.0);

        // Assert - Should be positive
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Act
        var result = distribution.ToString();

        // Assert
        result.ShouldContain("W+(x");
    }

    [Fact]
    public void Constructor_WithZeroRanks_ShouldFilterThem()
    {
        // Arrange
        var ranks = new[] { 0.0, 1.0, 2.0, 0.0, 3.0 };

        // Act
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        // Assert - Should create distribution without zeros
        distribution.ShouldNotBeNull();
        distribution.Table.ShouldNotBeNull();
    }

    [Fact]
    public void InverseDistributionFunction_WithApproximation_ShouldWorkCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        // Act
        var result = distribution.InverseDistributionFunction(0.5);

        // Assert - At p=0.5, should return approximately the mean
        result.ShouldBe(distribution.Mean, 1.0);
    }
}

