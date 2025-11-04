using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class NormalStaticTests
{
    [Fact]
    public void Function_AtZero_ShouldReturnHalf()
    {
        // Arrange & Act
        var result = Normal.Function(0.0);

        // Assert
        result.ShouldBe(0.5, 1e-10);
    }

    [Fact]
    public void Function_WithPositiveValue_ShouldReturnGreaterThanHalf()
    {
        // Arrange & Act
        var result = Normal.Function(1.0);

        // Assert
        result.ShouldBeGreaterThan(0.5);
        result.ShouldBeLessThan(1.0);
    }

    [Fact]
    public void Function_WithNegativeValue_ShouldReturnLessThanHalf()
    {
        // Arrange & Act
        var result = Normal.Function(-1.0);

        // Assert
        result.ShouldBeLessThan(0.5);
        result.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Function_WithSymmetricValues_ShouldSumToOne()
    {
        // Arrange
        const double value = 1.5;

        // Act
        var positive = Normal.Function(value);
        var negative = Normal.Function(-value);

        // Assert
        (positive + negative).ShouldBe(1.0, 1e-10);
    }

    [Fact]
    public void Function_WithLargePositiveValue_ShouldApproachOne()
    {
        // Arrange & Act
        var result = Normal.Function(5.0);

        // Assert
        result.ShouldBeGreaterThan(0.99);
    }

    [Fact]
    public void Function_WithLargeNegativeValue_ShouldApproachZero()
    {
        // Arrange & Act
        var result = Normal.Function(-5.0);

        // Assert
        result.ShouldBeLessThan(0.01);
    }

    [Fact]
    public void Complemented_AtZero_ShouldReturnHalf()
    {
        // Arrange & Act
        var result = Normal.Complemented(0.0);

        // Assert
        result.ShouldBe(0.5, 1e-10);
    }

    [Fact]
    public void Complemented_WithPositiveValue_ShouldReturnLessThanHalf()
    {
        // Arrange & Act
        var result = Normal.Complemented(1.0);

        // Assert
        result.ShouldBeLessThan(0.5);
        result.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Complemented_WithNegativeValue_ShouldReturnGreaterThanHalf()
    {
        // Arrange & Act
        var result = Normal.Complemented(-1.0);

        // Assert
        result.ShouldBeGreaterThan(0.5);
        result.ShouldBeLessThan(1.0);
    }

    [Fact]
    public void FunctionAndComplemented_ShouldSumToOne()
    {
        // Arrange
        const double value = 1.5;

        // Act
        var function = Normal.Function(value);
        var complemented = Normal.Complemented(value);

        // Assert
        (function + complemented).ShouldBe(1.0, 1e-10);
    }

    [Fact]
    public void Inverse_WithZero_ShouldReturnNegativeInfinity()
    {
        // Arrange & Act
        var result = Normal.Inverse(0.0);

        // Assert
        double.IsNegativeInfinity(result).ShouldBeTrue();
    }

    [Fact]
    public void Inverse_WithOne_ShouldReturnPositiveInfinity()
    {
        // Arrange & Act
        var result = Normal.Inverse(1.0);

        // Assert
        double.IsPositiveInfinity(result).ShouldBeTrue();
    }

    [Fact]
    public void Inverse_WithHalf_ShouldReturnZero()
    {
        // Arrange & Act
        var result = Normal.Inverse(0.5);

        // Assert
        result.ShouldBe(0.0, 1e-10);
    }

    [Fact]
    public void Inverse_WithValidProbability_ShouldReturnFiniteValue()
    {
        // Arrange & Act
        var result = Normal.Inverse(0.95);

        // Assert
        double.IsFinite(result).ShouldBeTrue();
        result.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Inverse_WithLowProbability_ShouldReturnNegativeValue()
    {
        // Arrange & Act
        var result = Normal.Inverse(0.05);

        // Assert
        double.IsFinite(result).ShouldBeTrue();
        result.ShouldBeLessThan(0.0);
    }

    [Fact]
    public void Inverse_WithNegativeProbability_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Normal.Inverse(-0.1));
    }

    [Fact]
    public void Inverse_WithProbabilityGreaterThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Normal.Inverse(1.1));
    }

    [Fact]
    public void Inverse_ShouldBeInverseOfFunction()
    {
        // Arrange
        const double probability = 0.75;

        // Act
        var zScore = Normal.Inverse(probability);
        var recoveredProbability = Normal.Function(zScore);

        // Assert
        recoveredProbability.ShouldBe(probability, 1e-10);
    }

    [Fact]
    public void Inverse_WithSymmetricProbabilities_ShouldReturnOppositeValues()
    {
        // Arrange
        const double p1 = 0.25;
        const double p2 = 0.75;

        // Act
        var z1 = Normal.Inverse(p1);
        var z2 = Normal.Inverse(p2);

        // Assert
        z1.ShouldBe(-z2, 1e-10);
    }

    [Fact]
    public void Inverse_WithMonotonicallyIncreasingProbabilities_ShouldReturnMonotonicallyIncreasingValues()
    {
        // Arrange
        const double p1 = 0.1;
        const double p2 = 0.5;
        const double p3 = 0.9;

        // Act
        var z1 = Normal.Inverse(p1);
        var z2 = Normal.Inverse(p2);
        var z3 = Normal.Inverse(p3);

        // Assert
        z1.ShouldBeLessThan(z2);
        z2.ShouldBeLessThan(z3);
    }
}

