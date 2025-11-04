using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class BetaTests
{
    [Fact]
    public void Incomplete_WithValidParameters_ShouldReturnValueBetweenZeroAndOne()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double x = 0.5;

        // Act
        var result = Beta.Incomplete(a, b, x);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0.0);
        result.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void Incomplete_WithXEqualsZero_ShouldReturnZero()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double x = 0.0;

        // Act
        var result = Beta.Incomplete(a, b, x);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void Incomplete_WithXEqualsOne_ShouldReturnOne()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double x = 1.0;

        // Act
        var result = Beta.Incomplete(a, b, x);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void Incomplete_WithALessThanOrEqualToZero_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Beta.Incomplete(0.0, 3.0, 0.5));
    }

    [Fact]
    public void Incomplete_WithBLessThanOrEqualToZero_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Beta.Incomplete(2.0, 0.0, 0.5));
    }

    [Fact]
    public void Incomplete_WithXLessThanZero_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Beta.Incomplete(2.0, 3.0, -0.1));
    }

    [Fact]
    public void Incomplete_WithXGreaterThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Beta.Incomplete(2.0, 3.0, 1.1));
    }

    [Fact]
    public void Incomplete_WithSymmetricParameters_ShouldReturnHalfAtMidpoint()
    {
        // Arrange
        const double a = 2.0;
        const double b = 2.0;
        const double x = 0.5;

        // Act
        var result = Beta.Incomplete(a, b, x);

        // Assert
        result.ShouldBe(0.5, 1e-10);
    }

    [Fact]
    public void Incomplete_WithDifferentParameters_ShouldReturnMonotonicallyIncreasing()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double x1 = 0.3;
        const double x2 = 0.5;
        const double x3 = 0.7;

        // Act
        var result1 = Beta.Incomplete(a, b, x1);
        var result2 = Beta.Incomplete(a, b, x2);
        var result3 = Beta.Incomplete(a, b, x3);

        // Assert
        result1.ShouldBeLessThan(result2);
        result2.ShouldBeLessThan(result3);
    }

    [Fact]
    public void IncompleteInverse_WithValidParameters_ShouldReturnValueBetweenZeroAndOne()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double y = 0.5;

        // Act
        var result = Beta.IncompleteInverse(a, b, y);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0.0);
        result.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void IncompleteInverse_WithYEqualsZero_ShouldReturnZero()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double y = 0.0;

        // Act
        var result = Beta.IncompleteInverse(a, b, y);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void IncompleteInverse_WithYEqualsOne_ShouldReturnOne()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double y = 1.0;

        // Act
        var result = Beta.IncompleteInverse(a, b, y);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void IncompleteInverse_ShouldBeInverseOfIncomplete()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double x = 0.5;

        // Act
        var y = Beta.Incomplete(a, b, x);
        var xRecovered = Beta.IncompleteInverse(a, b, y);

        // Assert
        xRecovered.ShouldBe(x, 1e-6);
    }

    [Fact]
    public void Incbcf_WithValidParameters_ShouldReturnPositiveValue()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double x = 0.5;

        // Act
        var result = Beta.Incbcf(a, b, x);

        // Assert
        result.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Incbd_WithValidParameters_ShouldReturnPositiveValue()
    {
        // Arrange
        const double a = 2.0;
        const double b = 3.0;
        const double x = 0.5;

        // Act
        var result = Beta.Incbd(a, b, x);

        // Assert
        result.ShouldBeGreaterThan(0.0);
    }
}

