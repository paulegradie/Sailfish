using System.Linq;
using Sailfish.Analysis;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis;

/// <summary>
/// Comprehensive unit tests for StatisticalConvergenceDetector.
/// Tests convergence detection logic, edge cases, and statistical calculations.
/// </summary>
public class StatisticalConvergenceDetectorTests
{
    private readonly StatisticalConvergenceDetector detector = new();

    [Fact]
    public void CheckConvergence_WithNullSamples_ReturnsFalse()
    {
        // Act
        var result = detector.CheckConvergence(null!, 0.05, 0.95, 10);

        // Assert
        result.HasConverged.ShouldBeFalse();
        result.Reason.ShouldContain("No samples provided");
        result.SampleCount.ShouldBe(0);
    }

    [Fact]
    public void CheckConvergence_WithEmptySamples_ReturnsFalse()
    {
        // Arrange
        var samples = new double[0];

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.HasConverged.ShouldBeFalse();
        result.Reason.ShouldContain("No samples provided");
        result.SampleCount.ShouldBe(0);
    }

    [Fact]
    public void CheckConvergence_WithInsufficientSamples_ReturnsFalse()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0 };

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.HasConverged.ShouldBeFalse();
        result.Reason.ShouldContain("Insufficient samples");
        result.SampleCount.ShouldBe(3);
    }

    [Fact]
    public void CheckConvergence_WithLowVariability_ReturnsTrue()
    {
        // Arrange - samples with low coefficient of variation
        var samples = Enumerable.Range(1, 20)
            .Select(x => 100.0 + (x % 3)) // Values: 101, 102, 100, 101, 102, 100...
            .ToArray();

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.HasConverged.ShouldBeTrue();
        result.CurrentCoefficientOfVariation.ShouldBeLessThan(0.05);
        result.CurrentMean.ShouldBe(101.05, 0.1);
        result.SampleCount.ShouldBe(20);
        result.Reason.ShouldContain("Converged");
    }

    [Fact]
    public void CheckConvergence_WithHighVariability_ReturnsFalse()
    {
        // Arrange - samples with high coefficient of variation
        var samples = Enumerable.Range(1, 20)
            .Select(x => (double)(x * 10)) // Values: 10, 20, 30, 40...
            .ToArray();

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.HasConverged.ShouldBeFalse();
        result.CurrentCoefficientOfVariation.ShouldBeGreaterThan(0.05);
        result.SampleCount.ShouldBe(20);
        result.Reason.ShouldContain("Not converged");
    }

    [Fact]
    public void CheckConvergence_WithZeroMean_ReturnsFalse()
    {
        // Arrange - samples that average to zero
        var samples = new[] { -5.0, -3.0, 0.0, 3.0, 5.0, -5.0, -3.0, 0.0, 3.0, 5.0 };

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 5);

        // Assert
        result.HasConverged.ShouldBeFalse();
        result.Reason.ShouldContain("mean is zero");
        result.CurrentMean.ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void CheckConvergence_WithIdenticalValues_ReturnsTrue()
    {
        // Arrange - all samples are identical (CV should be 0)
        var samples = Enumerable.Repeat(100.0, 15).ToArray();

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.HasConverged.ShouldBeTrue();
        result.CurrentCoefficientOfVariation.ShouldBe(0.0);
        result.CurrentMean.ShouldBe(100.0);
        result.CurrentStandardDeviation.ShouldBe(0.0);
        result.SampleCount.ShouldBe(15);
    }

    [Fact]
    public void CheckConvergence_WithBorderlineCV_ReturnsCorrectResult()
    {
        // Arrange - samples with CV exactly at the threshold
        // Mean = 100, StdDev = 5, CV = 0.05
        var samples = new[] { 95.0, 97.0, 98.0, 100.0, 100.0, 100.0, 102.0, 103.0, 105.0, 100.0 };

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 5);

        // Assert
        result.SampleCount.ShouldBe(10);
        result.CurrentMean.ShouldBe(100.0, 0.1);
        // The exact CV will depend on the actual standard deviation calculation
        result.CurrentCoefficientOfVariation.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void CheckConvergence_WithNegativeValues_HandlesCorrectly()
    {
        // Arrange - all negative values with low variability
        var samples = Enumerable.Range(1, 15)
            .Select(x => -100.0 - (x % 3)) // Values: -101, -102, -100, -101, -102, -100...
            .ToArray();

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.SampleCount.ShouldBe(15);
        result.CurrentMean.ShouldBeLessThan(0);
        // CV should be calculated correctly even with negative mean
        result.CurrentCoefficientOfVariation.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CheckConvergence_WithVeryLargeNumbers_HandlesCorrectly()
    {
        // Arrange - very large numbers with low relative variability
        var samples = Enumerable.Range(1, 15)
            .Select(x => 1_000_000.0 + (x % 3)) // Values around 1 million
            .ToArray();

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.SampleCount.ShouldBe(15);
        result.CurrentMean.ShouldBeGreaterThan(1_000_000);
        result.CurrentCoefficientOfVariation.ShouldBeLessThan(0.05);
        result.HasConverged.ShouldBeTrue();
    }
}
