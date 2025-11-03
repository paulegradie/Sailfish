using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class KolmogorovSmirnovDistributionTests
{
    [Fact]
    public void Constructor_WithValidSamples_ShouldCreateInstance()
    {
        // Arrange & Act
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Assert
        distribution.ShouldNotBeNull();
        distribution.NumberOfSamples.ShouldBe(100);
    }

    [Fact]
    public void Constructor_WithZeroSamples_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            new KolmogorovSmirnovDistribution(0));
        exception.ParamName.ShouldBe("samples");
    }

    [Fact]
    public void Constructor_WithNegativeSamples_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            new KolmogorovSmirnovDistribution(-10));
        exception.ParamName.ShouldBe("samples");
    }

    [Fact]
    public void NumberOfSamples_ShouldReturnConstructorValue()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(50);

        // Act & Assert
        distribution.NumberOfSamples.ShouldBe(50);
    }

    [Fact]
    public void Support_ShouldReturnCorrectRange()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act
        var support = distribution.Support;

        // Assert
        support.Min.ShouldBe(0.5 / 100); // 0.5 / n
        support.Max.ShouldBe(1.0);
    }

    [Fact]
    public void Mean_ShouldCalculateCorrectly()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act
        var mean = distribution.Mean;

        // Assert - Mean = 0.8687311606361592 / sqrt(n)
        var expected = 0.8687311606361592 / Math.Sqrt(100);
        mean.ShouldBe(expected);
    }

    [Fact]
    public void DistributionFunction_BelowSupport_ShouldReturnZero()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act
        var result = distribution.DistributionFunction(0.001);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void DistributionFunction_AboveSupport_ShouldReturnOne()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act
        var result = distribution.DistributionFunction(1.5);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void DistributionFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.DistributionFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void CumulativeFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            KolmogorovSmirnovDistribution.CumulativeFunction(100, double.NaN));
    }

    [Fact]
    public void CumulativeFunction_WithXGreaterThanOrEqualToOne_ShouldReturnOne()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.CumulativeFunction(100, 1.0);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void CumulativeFunction_WithXBelowMinimum_ShouldReturnZero()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.CumulativeFunction(100, 0.001);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void CumulativeFunction_WithNEqualsOne_ShouldUseSpecialFormula()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.CumulativeFunction(1, 0.6);

        // Assert - For n=1, CDF = 2*x - 1
        result.ShouldBe(2.0 * 0.6 - 1.0);
    }

    [Fact]
    public void ComplementaryDistributionFunction_WithXGreaterThanOrEqualToOne_ShouldReturnZero()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.ComplementaryDistributionFunction(100, 1.0);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void ComplementaryDistributionFunction_WithXBelowMinimum_ShouldReturnOne()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.ComplementaryDistributionFunction(100, 0.001);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void ComplementaryDistributionFunction_WithNEqualsOne_ShouldUseSpecialFormula()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.ComplementaryDistributionFunction(1, 0.6);

        // Assert - For n=1, CCDF = 2 - 2*x
        result.ShouldBe(2.0 - 2.0 * 0.6);
    }

    [Fact]
    public void OneSideDistributionFunction_WithSmallN_ShouldWorkCorrectly()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(10);

        // Act
        var result = distribution.OneSideDistributionFunction(0.3);

        // Assert - Should return a value between 0 and 1
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void OneSideUpperTail_WithLargeN_ShouldUseApproximation()
    {
        // Act - n > 200000 uses special approximation
        var result = KolmogorovSmirnovDistribution.OneSideUpperTail(250000, 0.01);

        // Assert - Should return a value between 0 and 1
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void OneSideUpperTail_WithSmallN_ShouldUseExactMethod()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.OneSideUpperTail(50, 0.1);

        // Assert - Should return a value between 0 and 1
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void PelzGood_ShouldReturnFiniteValue()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.PelzGood(100, 0.1);

        // Assert
        double.IsFinite(result).ShouldBeTrue();
    }

    [Fact]
    public void Pomeranz_ShouldReturnValueBetweenZeroAndOne()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.Pomeranz(50, 0.15);

        // Assert
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void Durbin_ShouldReturnPositiveValue()
    {
        // Act
        var result = KolmogorovSmirnovDistribution.Durbin(20, 0.2);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ToString_WithDefaultFormat_ShouldReturnFormattedString()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act
        var result = distribution.ToString();

        // Assert
        result.ShouldContain("KS(x");
        result.ShouldContain("n");
        result.ShouldContain("100");
    }

    [Fact]
    public void ToString_WithCustomFormat_ShouldFormatNumbers()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100.5);

        // Act
        var result = distribution.ToString("F1", null);

        // Assert
        result.ShouldContain("100.5");
    }

    [Fact]
    public void ProbabilityDensityFunction_ShouldThrowNotSupportedException()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act & Assert
        Should.Throw<NotSupportedException>(() =>
            distribution.ProbabilityDensityFunction(0.1));
    }

    [Fact]
    public void CumulativeFunction_WithSmallNAndSmallX_ShouldUseDurbin()
    {
        // Act - n <= 140 and number < 0.754693
        var result = KolmogorovSmirnovDistribution.CumulativeFunction(50, 0.05);

        // Assert
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void CumulativeFunction_WithSmallNAndMediumX_ShouldUsePomeranz()
    {
        // Act - n <= 140 and 0.754693 <= number < 4.0
        var result = KolmogorovSmirnovDistribution.CumulativeFunction(50, 0.15);

        // Assert
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void CumulativeFunction_WithLargeNAndSmallX_ShouldUseDurbin()
    {
        // Act - n > 140 and n <= 100000 and n * number * x <= 1.96
        var result = KolmogorovSmirnovDistribution.CumulativeFunction(200, 0.05);

        // Assert
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void CumulativeFunction_WithLargeNAndLargeX_ShouldUsePelzGood()
    {
        // Act - n > 100000
        var result = KolmogorovSmirnovDistribution.CumulativeFunction(150000, 0.01);

        // Assert
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void ComplementaryDistributionFunction_WithSmallNAndLargeX_ShouldUseOneSideUpperTail()
    {
        // Act - n <= 140 and number >= 4.0
        var result = KolmogorovSmirnovDistribution.ComplementaryDistributionFunction(50, 0.3);

        // Assert
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void ComplementaryDistributionFunction_WithLargeNAndLargeX_ShouldUseOneSideUpperTail()
    {
        // Act - n > 140 and number >= 2.2
        var result = KolmogorovSmirnovDistribution.ComplementaryDistributionFunction(200, 0.15);

        // Assert
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void DistributionFunction_WithTypicalValues_ShouldBeMonotonicallyIncreasing()
    {
        // Arrange
        var distribution = new KolmogorovSmirnovDistribution(100);

        // Act
        var cdf1 = distribution.DistributionFunction(0.05);
        var cdf2 = distribution.DistributionFunction(0.10);
        var cdf3 = distribution.DistributionFunction(0.15);

        // Assert - CDF should be monotonically increasing
        cdf2.ShouldBeGreaterThanOrEqualTo(cdf1);
        cdf3.ShouldBeGreaterThanOrEqualTo(cdf2);
    }

    [Fact]
    public void ComplementaryDistributionFunction_PlusCumulativeFunction_ShouldEqualOne()
    {
        // Arrange
        var n = 100.0;
        var x = 0.1;

        // Act
        var cdf = KolmogorovSmirnovDistribution.CumulativeFunction(n, x);
        var ccdf = KolmogorovSmirnovDistribution.ComplementaryDistributionFunction(n, x);

        // Assert - CDF + CCDF should equal 1
        (cdf + ccdf).ShouldBe(1.0, 1e-10);
    }

    [Fact]
    public void Constructor_WithFractionalSamples_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var distribution = new KolmogorovSmirnovDistribution(100.5);

        // Assert
        distribution.NumberOfSamples.ShouldBe(100.5);
    }
}

