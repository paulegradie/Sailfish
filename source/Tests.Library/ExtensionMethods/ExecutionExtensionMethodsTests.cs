using System;
using Sailfish.Attributes;
using Sailfish.Analysis;

using Sailfish.Extensions.Methods;
using Shouldly;
using Xunit;

namespace Tests.Library.ExtensionMethods;

/// <summary>
/// Comprehensive unit tests for ExecutionExtensionMethods.
/// Tests execution settings retrieval, attribute processing, and global override handling.
/// </summary>
public class ExecutionExtensionMethodsTests
{
    [Fact]
    public void RetrieveExecutionTestSettings_WithAllAttributes_ShouldReturnCorrectSettings()
    {
        // Arrange
        var type = typeof(TestClassWithAllAttributes);
        int? globalSampleSize = null;
        int? globalNumWarmupIterations = null;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.AsCsv.ShouldBeTrue();
        result.AsMarkdown.ShouldBeTrue();
        result.AsConsole.ShouldBeTrue(); // SuppressConsoleAttribute is NOT present, so AsConsole should be true
        result.SampleSize.ShouldBe(100);
        result.NumWarmupIterations.ShouldBe(20);
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithSuppressConsoleAttribute_ShouldSuppressConsole()
    {
        // Arrange
        var type = typeof(TestClassWithSuppressConsole);
        int? globalSampleSize = null;
        int? globalNumWarmupIterations = null;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.AsConsole.ShouldBeFalse(); // SuppressConsoleAttribute is present, so AsConsole should be false
        result.AsCsv.ShouldBeFalse();
        result.AsMarkdown.ShouldBeFalse();
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithNoAttributes_ShouldReturnDefaultSettings()
    {
        // Arrange
        var type = typeof(TestClassWithNoAttributes);
        int? globalSampleSize = null;
        int? globalNumWarmupIterations = null;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.AsCsv.ShouldBeFalse();
        result.AsMarkdown.ShouldBeFalse();
        result.AsConsole.ShouldBeTrue(); // Default when no SuppressConsoleAttribute
        result.SampleSize.ShouldBe(3); // Default from SailfishAttribute
        result.NumWarmupIterations.ShouldBe(3); // Default from SailfishAttribute
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithGlobalSampleSizeOverride_ShouldUseGlobalValue()
    {
        // Arrange
        var type = typeof(TestClassWithAllAttributes);
        int? globalSampleSize = 500;
        int? globalNumWarmupIterations = null;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.SampleSize.ShouldBe(500); // Global override
        result.NumWarmupIterations.ShouldBe(20); // From attribute
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithGlobalWarmupIterationsOverride_ShouldUseGlobalValue()
    {
        // Arrange
        var type = typeof(TestClassWithAllAttributes);
        int? globalSampleSize = null;
        int? globalNumWarmupIterations = 50;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.SampleSize.ShouldBe(100); // From attribute
        result.NumWarmupIterations.ShouldBe(50); // Global override
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithBothGlobalOverrides_ShouldUseBothGlobalValues()
    {
        // Arrange
        var type = typeof(TestClassWithAllAttributes);
        int? globalSampleSize = 300;
        int? globalNumWarmupIterations = 15;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.SampleSize.ShouldBe(300); // Global override
        result.NumWarmupIterations.ShouldBe(15); // Global override
        result.AsCsv.ShouldBeTrue(); // From attribute
        result.AsMarkdown.ShouldBeTrue(); // From attribute
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithOnlyMarkdownAttribute_ShouldReturnCorrectSettings()
    {
        // Arrange
        var type = typeof(TestClassWithOnlyMarkdown);
        int? globalSampleSize = null;
        int? globalNumWarmupIterations = null;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.AsMarkdown.ShouldBeTrue();
        result.AsCsv.ShouldBeFalse();
        result.AsConsole.ShouldBeTrue(); // Default when no SuppressConsoleAttribute
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithOnlyCsvAttribute_ShouldReturnCorrectSettings()
    {
        // Arrange
        var type = typeof(TestClassWithOnlyCsv);
        int? globalSampleSize = null;
        int? globalNumWarmupIterations = null;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.AsCsv.ShouldBeTrue();
        result.AsMarkdown.ShouldBeFalse();
        result.AsConsole.ShouldBeTrue(); // Default when no SuppressConsoleAttribute
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithZeroGlobalValues_ShouldUseGlobalValues()
    {
        // Arrange
        var type = typeof(TestClassWithAllAttributes);
        int? globalSampleSize = 0;
        int? globalNumWarmupIterations = 0;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.SampleSize.ShouldBe(0); // Global override even if zero
        result.NumWarmupIterations.ShouldBe(0); // Global override even if zero
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithCustomSampleSizeAndWarmup_ShouldReturnCorrectValues()
    {
        // Arrange
        var type = typeof(TestClassWithCustomValues);
        int? globalSampleSize = null;
        int? globalNumWarmupIterations = null;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.SampleSize.ShouldBe(75);
        result.NumWarmupIterations.ShouldBe(12);
    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithNegativeGlobalValues_ShouldUseGlobalValues()
    {
        // Arrange
        var type = typeof(TestClassWithAllAttributes);
        int? globalSampleSize = -1;
        int? globalNumWarmupIterations = -5;

        // Act
        var result = type.RetrieveExecutionTestSettings(globalSampleSize, globalNumWarmupIterations);

        // Assert
        result.ShouldNotBeNull();
        result.SampleSize.ShouldBe(-1); // Global override even if negative
        result.NumWarmupIterations.ShouldBe(-5); // Global override even if negative
    }

    // Test classes with various attribute combinations
    [Sailfish(SampleSize = 100, NumWarmupIterations = 20)]
    [WriteToMarkdown]
    [WriteToCsv]
    private class TestClassWithAllAttributes
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish]
    [SuppressConsole]
    private class TestClassWithSuppressConsole
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish]
    private class TestClassWithNoAttributes
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish]
    [WriteToMarkdown]
    private class TestClassWithOnlyMarkdown
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish]
    [WriteToCsv]
    private class TestClassWithOnlyCsv
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish(SampleSize = 75, NumWarmupIterations = 12)]
    private class TestClassWithCustomValues
    {
        [SailfishMethod]
        public void TestMethod() { }

    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithGlobalAdaptiveOverrides_ShouldOverrideAttributeValues()
    {
        // Arrange
        var type = typeof(TestClassWithAdaptiveAttributes);

        // Act
        var result = type.RetrieveExecutionTestSettings(
            globalSampleSize: null,
            globalNumWarmupIterations: null,
            globalUseAdaptiveSampling: true,
            globalTargetCoefficientOfVariation: 0.05,
            globalMaximumSampleSize: 500);

        // Assert
        result.ShouldNotBeNull();
        result.UseAdaptiveSampling.ShouldBeTrue();
        result.TargetCoefficientOfVariation.ShouldBe(0.05);
        result.MaximumSampleSize.ShouldBe(500);
    }

    [Sailfish(UseAdaptiveSampling = false, TargetCoefficientOfVariation = 0.10, MaximumSampleSize = 200)]
    private class TestClassWithAdaptiveAttributes
    {
        [SailfishMethod]
        public void TestMethod() { }


    }

    [Fact]
    public void RetrieveExecutionTestSettings_WithGlobalOutlierOverrides_ShouldOverrideSettings()
    {
        // Arrange
        var type = typeof(TestClassWithNoAttributes);

        // Act
        var result = type.RetrieveExecutionTestSettings(
            globalSampleSize: null,
            globalNumWarmupIterations: null,
            globalUseAdaptiveSampling: null,
            globalTargetCoefficientOfVariation: null,
            globalMaximumSampleSize: null,
            globalUseConfigurableOutlierDetection: true,
            globalOutlierStrategy: OutlierStrategy.RemoveUpper);

        // Assert
        result.ShouldNotBeNull();
        result.UseConfigurableOutlierDetection.ShouldBeTrue();
        result.OutlierStrategy.ShouldBe(OutlierStrategy.RemoveUpper);
    }

}
