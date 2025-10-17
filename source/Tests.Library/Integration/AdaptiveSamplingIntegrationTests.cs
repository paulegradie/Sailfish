using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;
using Shouldly;
using Xunit;

namespace Tests.Library.Integration;

/// <summary>
/// Integration tests for adaptive sampling functionality.
/// Tests end-to-end behavior with real test classes and execution.
/// </summary>
public class AdaptiveSamplingIntegrationTests
{
    [Fact]
    public void AdaptiveSamplingAttribute_WithValidConfiguration_ShouldSetProperties()
    {
        // Arrange
        var testType = typeof(LowVariabilityTestClass);

        // Act
        var executionSettings = testType.RetrieveExecutionTestSettings(null, null);

        // Assert
        executionSettings.UseAdaptiveSampling.ShouldBeTrue();
        executionSettings.TargetCoefficientOfVariation.ShouldBe(0.05);
        executionSettings.MaximumSampleSize.ShouldBe(100);
        executionSettings.MinimumSampleSize.ShouldBe(10);
        executionSettings.ConfidenceLevel.ShouldBe(0.95);
    }

    [Fact]
    public void FixedSamplingAttribute_WithoutAdaptiveSettings_ShouldUseDefaults()
    {
        // Arrange
        var testType = typeof(FixedSamplingTestClass);

        // Act
        var executionSettings = testType.RetrieveExecutionTestSettings(null, null);

        // Assert
        executionSettings.UseAdaptiveSampling.ShouldBeFalse();
        executionSettings.TargetCoefficientOfVariation.ShouldBe(0.05);
        executionSettings.MaximumSampleSize.ShouldBe(1000);
        executionSettings.MinimumSampleSize.ShouldBe(10);
    }

    [Fact]
    public void AdaptiveSamplingAttribute_WithCustomConfiguration_ShouldUseCustomValues()
    {
        // Arrange
        var testType = typeof(CustomAdaptiveTestClass);

        // Act
        var executionSettings = testType.RetrieveExecutionTestSettings(null, null);

        // Assert
        executionSettings.UseAdaptiveSampling.ShouldBeTrue();
        executionSettings.TargetCoefficientOfVariation.ShouldBe(0.02);
        executionSettings.MaximumSampleSize.ShouldBe(500);
    }

    [Fact]
    public void BackwardCompatibility_ExistingTestClass_ShouldWorkUnchanged()
    {
        // Arrange
        var testType = typeof(ExistingTestClass);

        // Act
        var executionSettings = testType.RetrieveExecutionTestSettings(null, null);

        // Assert
        executionSettings.UseAdaptiveSampling.ShouldBeFalse();
        executionSettings.SampleSize.ShouldBe(5);
        executionSettings.NumWarmupIterations.ShouldBe(2);
    }

    [Fact]
    public void GlobalOverrides_WithAdaptiveSampling_ShouldRespectOverrides()
    {
        // Arrange
        var testType = typeof(LowVariabilityTestClass);

        // Act
        var executionSettings = testType.RetrieveExecutionTestSettings(globalSampleSize: 50, globalNumWarmupIterations: 5);

        // Assert
        executionSettings.UseAdaptiveSampling.ShouldBeTrue();
        executionSettings.SampleSize.ShouldBe(50); // Global override
        executionSettings.NumWarmupIterations.ShouldBe(5); // Global override
        executionSettings.TargetCoefficientOfVariation.ShouldBe(0.05); // From attribute
    }
}

#region Test Classes for Integration Testing

/// <summary>
/// Test class with adaptive sampling enabled and low variability.
/// Should converge quickly in real execution.
/// </summary>
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 100)]
public class LowVariabilityTestClass
{
    [SailfishMethod]
    public async Task ConsistentMethod()
    {
        // Simulate a method with very consistent timing
        await Task.Delay(10); // Always 10ms
    }
}

/// <summary>
/// Test class with adaptive sampling enabled and high variability.
/// Should reach maximum iterations in real execution.
/// </summary>
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 50)]
public class HighVariabilityTestClass
{
    private static int counter = 0;

    [SailfishMethod]
    public async Task VariableMethod()
    {
        // Simulate a method with high variability
        var delay = (counter++ % 10) * 5; // 0, 5, 10, 15, 20, 25, 30, 35, 40, 45ms
        await Task.Delay(delay);
    }
}

/// <summary>
/// Test class with custom adaptive sampling configuration.
/// </summary>
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.02, MaximumSampleSize = 500)]
public class CustomAdaptiveTestClass
{
    [SailfishMethod]
    public async Task CustomConfigMethod()
    {
        await Task.Delay(5);
    }
}

/// <summary>
/// Traditional fixed sampling test class for backward compatibility testing.
/// </summary>
[Sailfish(SampleSize = 5, NumWarmupIterations = 2)]
public class FixedSamplingTestClass
{
    [SailfishMethod]
    public async Task FixedSamplingMethod()
    {
        await Task.Delay(10);
    }
}

/// <summary>
/// Existing test class without any adaptive sampling configuration.
/// Should maintain existing behavior.
/// </summary>
[Sailfish(SampleSize = 5, NumWarmupIterations = 2)]
public class ExistingTestClass
{
    [SailfishMethod]
    public async Task ExistingMethod()
    {
        await Task.Delay(15);
    }
}

/// <summary>
/// Test class with disabled adaptive sampling explicitly.
/// </summary>
[Sailfish(UseAdaptiveSampling = false, SampleSize = 8)]
public class DisabledAdaptiveTestClass
{
    [SailfishMethod]
    public async Task DisabledAdaptiveMethod()
    {
        await Task.Delay(12);
    }
}

#endregion
