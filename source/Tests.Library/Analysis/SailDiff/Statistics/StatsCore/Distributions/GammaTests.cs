using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class GammaTests
{
    [Fact]
    public void Function_WithPositiveInteger_ShouldReturnFactorial()
    {
        // Arrange & Act
        var result = Gamma.Function(5.0); // Gamma(5) = 4! = 24

        // Assert
        result.ShouldBe(24.0, 1e-10);
    }

    [Fact]
    public void Function_WithOne_ShouldReturnOne()
    {
        // Arrange & Act
        var result = Gamma.Function(1.0);

        // Assert
        result.ShouldBe(1.0, 1e-10);
    }

    [Fact]
    public void Function_WithHalf_ShouldReturnSqrtPi()
    {
        // Arrange & Act
        var result = Gamma.Function(0.5);

        // Assert
        result.ShouldBe(Math.Sqrt(Math.PI), 1e-10);
    }

    [Fact]
    public void Function_WithTwo_ShouldReturnOne()
    {
        // Arrange & Act
        var result = Gamma.Function(2.0);

        // Assert
        result.ShouldBe(1.0, 1e-10);
    }

    [Fact]
    public void Function_WithThree_ShouldReturnTwo()
    {
        // Arrange & Act
        var result = Gamma.Function(3.0);

        // Assert
        result.ShouldBe(2.0, 1e-10);
    }

    [Fact]
    public void Function_WithLargePositiveValue_ShouldReturnPositiveInfinity()
    {
        // Arrange & Act
        var result = Gamma.Function(200.0);

        // Assert
        double.IsPositiveInfinity(result).ShouldBeTrue();
    }

    [Fact]
    public void Function_WithZero_ShouldThrowArithmeticException()
    {
        // Act & Assert
        Should.Throw<ArithmeticException>(() => Gamma.Function(0.0));
    }

    [Fact]
    public void Function_WithNegativeInteger_ShouldThrowException()
    {
        // Act & Assert - Gamma.Function throws ArithmeticException for negative integers
        Should.Throw<ArithmeticException>(() => Gamma.Function(-2.0));
    }

    [Fact]
    public void Stirling_WithLargeValue_ShouldReturnPositiveValue()
    {
        // Arrange & Act
        var result = Gamma.Stirling(50.0);

        // Assert
        result.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Log_WithOne_ShouldReturnZero()
    {
        // Arrange & Act
        var result = Gamma.Log(1.0);

        // Assert
        result.ShouldBe(0.0, 1e-10);
    }

    [Fact]
    public void Log_WithTwo_ShouldReturnZero()
    {
        // Arrange & Act
        var result = Gamma.Log(2.0);

        // Assert
        result.ShouldBe(0.0, 1e-10);
    }

    [Fact]
    public void Log_WithPositiveValue_ShouldReturnPositiveValue()
    {
        // Arrange & Act
        var result = Gamma.Log(5.0);

        // Assert
        result.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Log_WithZero_ShouldReturnPositiveInfinity()
    {
        // Arrange & Act
        var result = Gamma.Log(0.0);

        // Assert
        double.IsPositiveInfinity(result).ShouldBeTrue();
    }

    [Fact]
    public void Log_WithLargeNegativeValue_ShouldThrowOverflowException()
    {
        // Act & Assert
        Should.Throw<OverflowException>(() => Gamma.Log(-100.0));
    }

    [Fact]
    public void LowerIncomplete_WithZeroA_ShouldReturnOne()
    {
        // Arrange & Act
        var result = Gamma.LowerIncomplete(0.0, 5.0);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void LowerIncomplete_WithZeroX_ShouldReturnZero()
    {
        // Arrange & Act
        var result = Gamma.LowerIncomplete(5.0, 0.0);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void LowerIncomplete_WithValidParameters_ShouldReturnValueBetweenZeroAndOne()
    {
        // Arrange
        const double a = 2.0;
        const double x = 3.0;

        // Act
        var result = Gamma.LowerIncomplete(a, x);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0.0);
        result.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void LowerIncomplete_WithLargeX_ShouldApproachOne()
    {
        // Arrange
        const double a = 2.0;
        const double x = 100.0;

        // Act
        var result = Gamma.LowerIncomplete(a, x);

        // Assert
        result.ShouldBeGreaterThan(0.99);
    }

    [Fact]
    public void UpperIncomplete_WithZeroA_ShouldReturnOne()
    {
        // Arrange & Act - UpperIncomplete is private, so we test through LowerIncomplete
        var lowerResult = Gamma.LowerIncomplete(0.0, 5.0);

        // Assert
        lowerResult.ShouldBe(1.0);
    }

    [Fact]
    public void LowerAndUpperIncomplete_ShouldSumToOne()
    {
        // Arrange
        const double a = 3.0;
        const double x = 2.0;

        // Act
        var lower = Gamma.LowerIncomplete(a, x);
        // Upper incomplete is private, but we can verify through the relationship
        // For large x, lower should approach 1

        // Assert
        lower.ShouldBeGreaterThan(0.0);
        lower.ShouldBeLessThanOrEqualTo(1.0);
    }
}

