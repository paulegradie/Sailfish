using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class NormalDistributionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var distribution = new NormalDistribution(0, 1);

        // Assert
        distribution.ShouldNotBeNull();
        distribution.Mean.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithCustomMeanAndStdDev_ShouldSetProperties()
    {
        // Arrange & Act
        var distribution = new NormalDistribution(10.5, 2.5);

        // Assert
        distribution.Mean.ShouldBe(10.5);
    }

    [Fact]
    public void Mean_ShouldReturnConstructorValue()
    {
        // Arrange
        var distribution = new NormalDistribution(5.0, 1.0);

        // Act & Assert
        distribution.Mean.ShouldBe(5.0);
    }

    [Fact]
    public void Support_ShouldReturnInfiniteRange()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var support = distribution.Support;

        // Assert
        support.Min.ShouldBe(double.NegativeInfinity);
        support.Max.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void DistributionFunction_AtMean_ShouldReturnHalf()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.DistributionFunction(0);

        // Assert
        result.ShouldBe(0.5, 1e-10);
    }

    [Fact]
    public void DistributionFunction_AtNegativeInfinity_ShouldReturnZero()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.DistributionFunction(double.NegativeInfinity);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void DistributionFunction_AtPositiveInfinity_ShouldReturnOne()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.DistributionFunction(double.PositiveInfinity);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void DistributionFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.DistributionFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void ComplementaryDistributionFunction_AtMean_ShouldReturnHalf()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.ComplementaryDistributionFunction(0);

        // Assert
        result.ShouldBe(0.5, 1e-10);
    }

    [Fact]
    public void ComplementaryDistributionFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.ComplementaryDistributionFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void ProbabilityDensityFunction_AtMean_ShouldReturnMaximum()
    {
        // Arrange - Standard normal distribution
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.ProbabilityDensityFunction(0);

        // Assert - PDF at mean for standard normal is 1/sqrt(2*pi) ≈ 0.3989
        result.ShouldBe(0.3989422804014327, 1e-10);
    }

    [Fact]
    public void ProbabilityDensityFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.ProbabilityDensityFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void LogProbabilityDensityFunction_AtMean_ShouldReturnLogOfPDF()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.LogProbabilityDensityFunction(0);
        var expected = Math.Log(0.3989422804014327);

        // Assert
        result.ShouldBe(expected, 1e-10);
    }

    [Fact]
    public void LogProbabilityDensityFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.LogProbabilityDensityFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void InverseDistributionFunction_WithHalf_ShouldReturnMean()
    {
        // Arrange
        var distribution = new NormalDistribution(5.0, 2.0);

        // Act
        var result = distribution.InverseDistributionFunction(0.5);

        // Assert
        result.ShouldBe(5.0, 1e-10);
    }

    [Fact]
    public void InverseDistributionFunction_WithZero_ShouldReturnNegativeInfinity()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.InverseDistributionFunction(0.0);

        // Assert
        result.ShouldBe(double.NegativeInfinity);
    }

    [Fact]
    public void InverseDistributionFunction_WithOne_ShouldReturnPositiveInfinity()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.InverseDistributionFunction(1.0);

        // Assert
        result.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void InverseDistributionFunction_WithInvalidProbability_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.InverseDistributionFunction(1.5));
        exception.ParamName.ShouldBe("p");
    }

    [Fact]
    public void InverseDistributionFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.InverseDistributionFunction(double.NaN));
        exception.ParamName.ShouldBe("p");
    }

    [Fact]
    public void ToString_WithDefaultFormat_ShouldReturnFormattedString()
    {
        // Arrange
        var distribution = new NormalDistribution(0, 1);

        // Act
        var result = distribution.ToString();

        // Assert
        result.ShouldContain("N(x");
        result.ShouldContain("μ");
        result.ShouldContain("σ²");
    }

    [Fact]
    public void ToString_WithCustomFormat_ShouldFormatNumbers()
    {
        // Arrange
        var distribution = new NormalDistribution(10.5, 2.5);

        // Act
        var result = distribution.ToString("F2", null);

        // Assert
        result.ShouldContain("10.50");
        result.ShouldContain("6.25"); // variance = 2.5^2
    }

    [Fact]
    public void Generate_ShouldProduceSamplesWithCorrectMean()
    {
        // Arrange
        var distribution = new NormalDistribution(10.0, 2.0);
        var random = new Random(42);

        // Act
        var samples = distribution.Generate(10000, random);

        // Assert
        samples.Length.ShouldBe(10000);
        var mean = samples.Average();
        mean.ShouldBe(10.0, 0.1); // Allow some variance
    }

    [Fact]
    public void DistributionFunction_WithCustomMeanAndStdDev_ShouldWorkCorrectly()
    {
        // Arrange
        var distribution = new NormalDistribution(100, 15);

        // Act
        var result = distribution.DistributionFunction(100);

        // Assert
        result.ShouldBe(0.5, 1e-10);
    }
}

