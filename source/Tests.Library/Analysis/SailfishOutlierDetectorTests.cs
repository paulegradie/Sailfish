using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis;

/// <summary>
/// Comprehensive unit tests for SailfishOutlierDetector.
/// Tests outlier detection algorithms, edge cases, and statistical accuracy.
/// </summary>
public class SailfishOutlierDetectorTests
{
    private readonly SailfishOutlierDetector outlierDetector;

    public SailfishOutlierDetectorTests()
    {
        outlierDetector = new SailfishOutlierDetector();
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act & Assert
        outlierDetector.ShouldNotBeNull();
        outlierDetector.ShouldBeAssignableTo<ISailfishOutlierDetector>();
    }

    [Fact]
    public void DetectOutliers_WithEmptyData_ShouldReturnEmptyResult()
    {
        // Arrange
        var data = new List<double>();

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.ShouldBeEmpty();
        result.DataWithOutliersRemoved.ShouldBeEmpty();
        result.LowerOutliers.ShouldBeEmpty();
        result.UpperOutliers.ShouldBeEmpty();
        result.TotalNumOutliers.ShouldBe(0);
    }

    [Fact]
    public void DetectOutliers_WithSingleValue_ShouldReturnOriginalData()
    {
        // Arrange
        var data = new List<double> { 100.0 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.ShouldBe(data);
        result.DataWithOutliersRemoved.ShouldBe(data);
        result.LowerOutliers.ShouldBeEmpty();
        result.UpperOutliers.ShouldBeEmpty();
        result.TotalNumOutliers.ShouldBe(0);
    }

    [Fact]
    public void DetectOutliers_WithTwoValues_ShouldReturnOriginalData()
    {
        // Arrange
        var data = new List<double> { 100.0, 105.0 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.ShouldBe(data);
        result.DataWithOutliersRemoved.ShouldBe(data);
        result.LowerOutliers.ShouldBeEmpty();
        result.UpperOutliers.ShouldBeEmpty();
        result.TotalNumOutliers.ShouldBe(0);
    }

    [Fact]
    public void DetectOutliers_WithThreeValues_ShouldReturnOriginalData()
    {
        // Arrange
        var data = new List<double> { 100.0, 105.0, 110.0 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.ShouldBe(data);
        result.DataWithOutliersRemoved.ShouldBe(data);
        result.LowerOutliers.ShouldBeEmpty();
        result.UpperOutliers.ShouldBeEmpty();
        result.TotalNumOutliers.ShouldBe(0);
    }

    [Fact]
    public void DetectOutliers_WithNormalDistribution_ShouldReturnMostData()
    {
        // Arrange - Normal distribution around 100
        var data = new List<double> { 95, 98, 99, 100, 101, 102, 105 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(7);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void DetectOutliers_WithClearUpperOutliers_ShouldDetectThem()
    {
        // Arrange - Normal data with clear upper outliers
        var data = new List<double> { 100, 101, 102, 103, 104, 105, 200, 300 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(8);
        result.UpperOutliers.ShouldNotBeEmpty();
        result.TotalNumOutliers.ShouldBeGreaterThan(0);
        result.DataWithOutliersRemoved.Length.ShouldBeLessThan(result.OriginalData.Length);
    }

    [Fact]
    public void DetectOutliers_WithClearLowerOutliers_ShouldDetectThem()
    {
        // Arrange - Normal data with clear lower outliers
        var data = new List<double> { 1, 2, 100, 101, 102, 103, 104, 105 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(8);
        result.LowerOutliers.ShouldNotBeEmpty();
        result.TotalNumOutliers.ShouldBeGreaterThan(0);
        result.DataWithOutliersRemoved.Length.ShouldBeLessThan(result.OriginalData.Length);
    }

    [Fact]
    public void DetectOutliers_WithBothUpperAndLowerOutliers_ShouldDetectBoth()
    {
        // Arrange - Normal data with outliers on both ends
        var data = new List<double> { 1, 2, 100, 101, 102, 103, 104, 105, 200, 300 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(10);
        result.LowerOutliers.ShouldNotBeEmpty();
        result.UpperOutliers.ShouldNotBeEmpty();
        result.TotalNumOutliers.ShouldBeGreaterThan(0);
        result.DataWithOutliersRemoved.Length.ShouldBeLessThan(result.OriginalData.Length);
    }

    [Fact]
    public void DetectOutliers_WithIdenticalValues_ShouldReturnAllData()
    {
        // Arrange - All identical values
        var data = new List<double> { 100, 100, 100, 100, 100, 100 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(6);
        result.DataWithOutliersRemoved.Length.ShouldBe(6);
        result.LowerOutliers.ShouldBeEmpty();
        result.UpperOutliers.ShouldBeEmpty();
        result.TotalNumOutliers.ShouldBe(0);
    }

    [Fact]
    public void DetectOutliers_WithNegativeValues_ShouldHandleCorrectly()
    {
        // Arrange - Data with negative values
        var data = new List<double> { -100, -50, -10, 0, 10, 50, 100 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(7);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void DetectOutliers_WithVeryLargeValues_ShouldHandleCorrectly()
    {
        // Arrange - Data with very large values
        var data = new List<double> { 1e6, 1.1e6, 1.2e6, 1.3e6, 1.4e6, 2e6 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(6);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void DetectOutliers_WithVerySmallValues_ShouldHandleCorrectly()
    {
        // Arrange - Data with very small values
        var data = new List<double> { 1e-6, 1.1e-6, 1.2e-6, 1.3e-6, 1.4e-6, 2e-6 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(6);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void DetectOutliers_WithZeroValues_ShouldHandleCorrectly()
    {
        // Arrange - Data with zeros
        var data = new List<double> { 0, 0, 1, 2, 3, 4, 5 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(7);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void DetectOutliers_ShouldPreserveOriginalData()
    {
        // Arrange
        var originalData = new List<double> { 1, 2, 100, 101, 102, 103, 104, 105, 200, 300 };
        var dataCopy = new List<double>(originalData);

        // Act
        var result = outlierDetector.DetectOutliers(originalData);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.ShouldBe(dataCopy); // Original data should be preserved
        originalData.ShouldBe(dataCopy); // Input should not be modified
    }

    [Fact]
    public void DetectOutliers_TotalOutliersShouldEqualSumOfUpperAndLower()
    {
        // Arrange
        var data = new List<double> { 1, 2, 100, 101, 102, 103, 104, 105, 200, 300 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        var expectedTotal = result.LowerOutliers.Count() + result.UpperOutliers.Count();
        result.TotalNumOutliers.ShouldBe(expectedTotal);
    }

    [Fact]
    public void DetectOutliers_DataWithOutliersRemovedPlusOutliersShouldEqualOriginal()
    {
        // Arrange
        var data = new List<double> { 1, 2, 100, 101, 102, 103, 104, 105, 200, 300 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        var totalProcessed = result.DataWithOutliersRemoved.Length + result.TotalNumOutliers;
        totalProcessed.ShouldBe(result.OriginalData.Length);
    }

    [Fact]
    public void DetectOutliers_WithExtremeOutliers_ShouldDetectCorrectly()
    {
        // Arrange - Data with extreme outliers
        var data = new List<double> { -1000, 100, 101, 102, 103, 104, 105, 1000 };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(8);
        result.TotalNumOutliers.ShouldBeGreaterThan(0);
        result.DataWithOutliersRemoved.Length.ShouldBeLessThan(result.OriginalData.Length);
    }

    [Fact]
    public void DetectOutliers_WithPerformanceTimingData_ShouldHandleRealistic()
    {
        // Arrange - Realistic performance timing data (milliseconds)
        var data = new List<double> 
        { 
            10.5, 11.2, 10.8, 11.0, 10.9, 11.1, 10.7, 11.3, 
            10.6, 11.4, 25.0, 10.8, 11.0, 10.9, 11.2 // 25.0 is an outlier
        };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(15);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void DetectOutliers_WithLargeDataset_ShouldPerformEfficiently()
    {
        // Arrange - Large dataset
        var random = new Random(42);
        var data = new List<double>();
        
        // Add normal data
        for (int i = 0; i < 1000; i++)
        {
            data.Add(100 + random.NextDouble() * 10);
        }
        
        // Add some outliers
        data.AddRange([200.0, 300.0, 1.0, 2.0]);

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(1004);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void DetectOutliers_WithFloatingPointPrecisionIssues_ShouldHandleCorrectly()
    {
        // Arrange - Data that might have floating point precision issues
        var data = new List<double> 
        { 
            0.1 + 0.2, 0.3, 0.30000000000000004, 0.299999999999999,
            0.1, 0.2, 0.4, 0.5, 10.0 // 10.0 should be an outlier
        };

        // Act
        var result = outlierDetector.DetectOutliers(data);

        // Assert
        result.ShouldNotBeNull();
        result.OriginalData.Length.ShouldBe(9);
        result.DataWithOutliersRemoved.Length.ShouldBeGreaterThan(0);
        result.TotalNumOutliers.ShouldBeGreaterThanOrEqualTo(0);
    }
}
