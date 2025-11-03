using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class EmpiricalDistributionTests
{
    [Fact]
    public void Constructor_WithValidSamples_ShouldCreateInstance()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Assert
        distribution.ShouldNotBeNull();
    }

    [Fact]
    public void Mean_ShouldReturnSampleMean()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act & Assert
        distribution.Mean.ShouldBe(3.0);
    }

    [Fact]
    public void Mean_WithDifferentSamples_ShouldCalculateCorrectly()
    {
        // Arrange
        var samples = new[] { 10.0, 20.0, 30.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act & Assert
        distribution.Mean.ShouldBe(20.0);
    }

    [Fact]
    public void Support_ShouldReturnInfiniteRange()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var support = distribution.Support;

        // Assert
        support.Min.ShouldBe(double.NegativeInfinity);
        support.Max.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void DistributionFunction_BelowAllSamples_ShouldReturnZero()
    {
        // Arrange
        var samples = new[] { 5.0, 10.0, 15.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var result = distribution.DistributionFunction(0.0);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void DistributionFunction_AboveAllSamples_ShouldReturnOne()
    {
        // Arrange
        var samples = new[] { 5.0, 10.0, 15.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var result = distribution.DistributionFunction(20.0);

        // Assert
        result.ShouldBe(1.0);
    }

    [Fact]
    public void DistributionFunction_AtMedianSample_ShouldReturnCorrectProportion()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var result = distribution.DistributionFunction(3.0);

        // Assert - 3 out of 5 samples are <= 3.0
        result.ShouldBe(0.6);
    }

    [Fact]
    public void DistributionFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.DistributionFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void ProbabilityDensityFunction_ShouldReturnPositiveValue()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var result = distribution.ProbabilityDensityFunction(3.0);

        // Assert - PDF should be positive
        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ProbabilityDensityFunction_AtSamplePoint_ShouldBeHigher()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution = new EmpiricalDistribution(samples, 0.5);

        // Act
        var pdfAtSample = distribution.ProbabilityDensityFunction(3.0);
        var pdfFarAway = distribution.ProbabilityDensityFunction(100.0);

        // Assert - PDF should be higher near sample points
        pdfAtSample.ShouldBeGreaterThan(pdfFarAway);
    }

    [Fact]
    public void ProbabilityDensityFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.ProbabilityDensityFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void ProbabilityDensityFunction_WithDifferentSmoothing_ShouldAffectResult()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution1 = new EmpiricalDistribution(samples, 0.5);
        var distribution2 = new EmpiricalDistribution(samples, 2.0);

        // Act
        var pdf1 = distribution1.ProbabilityDensityFunction(3.0);
        var pdf2 = distribution2.ProbabilityDensityFunction(3.0);

        // Assert - Different smoothing should give different results
        pdf1.ShouldNotBe(pdf2);
    }

    [Fact]
    public void ToString_WithDefaultFormat_ShouldReturnFormattedString()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var result = distribution.ToString();

        // Assert
        result.ShouldContain("Fn(x");
    }

    [Fact]
    public void ToString_WithCustomFormat_ShouldReturnFormattedString()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var result = distribution.ToString("F2", null);

        // Assert
        result.ShouldContain("Fn(x");
    }

    [Fact]
    public void DistributionFunction_WithSingleSample_ShouldWorkCorrectly()
    {
        // Arrange
        var samples = new[] { 5.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var belowSample = distribution.DistributionFunction(4.0);
        var atSample = distribution.DistributionFunction(5.0);
        var aboveSample = distribution.DistributionFunction(6.0);

        // Assert
        belowSample.ShouldBe(0.0);
        atSample.ShouldBe(1.0);
        aboveSample.ShouldBe(1.0);
    }

    [Fact]
    public void DistributionFunction_WithDuplicateSamples_ShouldCountAll()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 2.0, 3.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var result = distribution.DistributionFunction(2.0);

        // Assert - 3 out of 4 samples are <= 2.0
        result.ShouldBe(0.75);
    }

    [Fact]
    public void ProbabilityDensityFunction_WithSmallSmoothing_ShouldBeMorePeaked()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var smallSmoothing = new EmpiricalDistribution(samples, 0.1);
        var largeSmoothing = new EmpiricalDistribution(samples, 5.0);

        // Act
        var pdfSmall = smallSmoothing.ProbabilityDensityFunction(3.0);
        var pdfLarge = largeSmoothing.ProbabilityDensityFunction(3.0);

        // Assert - Smaller smoothing should give higher peak at sample points
        pdfSmall.ShouldBeGreaterThan(pdfLarge);
    }

    [Fact]
    public void Constructor_WithLargeSampleSet_ShouldCalculateMeanCorrectly()
    {
        // Arrange
        var samples = Enumerable.Range(1, 100).Select(x => (double)x).ToArray();

        // Act
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Assert
        distribution.Mean.ShouldBe(50.5);
    }

    [Fact]
    public void DistributionFunction_ShouldBeMonotonicallyIncreasing()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution = new EmpiricalDistribution(samples, 1.0);

        // Act
        var cdf1 = distribution.DistributionFunction(1.0);
        var cdf2 = distribution.DistributionFunction(2.0);
        var cdf3 = distribution.DistributionFunction(3.0);

        // Assert - CDF should be monotonically increasing
        cdf2.ShouldBeGreaterThanOrEqualTo(cdf1);
        cdf3.ShouldBeGreaterThanOrEqualTo(cdf2);
    }
}

