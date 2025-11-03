# Sailfish Adaptive Sampling Implementation Plan

## 🎯 Overview

This plan implements adaptive sampling in Sailfish, allowing tests to continue until statistical convergence is achieved rather than running a fixed number of iterations. The implementation leverages Sailfish's existing statistical infrastructure and maintains full backward compatibility.

## 📋 Prerequisites

**Required Knowledge:**
- Sailfish's execution pipeline (`TestCaseIterator`, `PerformanceTimer`)
- Existing statistical analysis (`SailfishOutlierDetector`, confidence intervals)
- Configuration system (`IExecutionSettings`, `RunSettings`)

**Key Files to Understand:**
- `source/Sailfish/Execution/TestCaseIterator.cs` - Main iteration logic
- `source/Sailfish/Execution/ExecutionSettings.cs` - Configuration
- `source/Sailfish/Analysis/SailfishOutlierDetector.cs` - Statistical analysis
- `source/Sailfish/Execution/PerformanceTimer.cs` - Timing collection

## 🔧 Implementation Steps

### **Step 1: Extend Configuration (Day 1, Morning)**

#### **1.1 Add Adaptive Sampling Settings to IExecutionSettings**

**File:** `source/Sailfish/Contracts/Public/Models/IExecutionSettings.cs`

**Changes:**
```csharp
public interface IExecutionSettings
{
    // Existing properties...
    
    // NEW: Adaptive Sampling Configuration
    bool UseAdaptiveSampling { get; set; }
    double TargetCoefficientOfVariation { get; set; }
    int MinimumSampleSize { get; set; }
    int MaximumSampleSize { get; set; }
    double ConfidenceLevel { get; set; }
}
```

**Implementation Notes:**
- `UseAdaptiveSampling`: Feature toggle for backward compatibility
- `TargetCoefficientOfVariation`: Default 0.05 (5% CV)
- `MinimumSampleSize`: Default 10 (ensure statistical validity)
- `MaximumSampleSize`: Default 1000 (prevent infinite loops)
- `ConfidenceLevel`: Default 0.95 (95% confidence)

#### **1.2 Update ExecutionSettings Implementation**

**File:** `source/Sailfish/Execution/ExecutionSettings.cs`

**Changes:**
```csharp
public class ExecutionSettings : IExecutionSettings
{
    // Existing properties...
    
    // NEW: Adaptive Sampling Properties
    public bool UseAdaptiveSampling { get; set; } = false;
    public double TargetCoefficientOfVariation { get; set; } = 0.05;
    public int MinimumSampleSize { get; set; } = 10;
    public int MaximumSampleSize { get; set; } = 1000;
    public double ConfidenceLevel { get; set; } = 0.95;
}
```

#### **1.3 Add Attribute Support**

**File:** `source/Sailfish/Attributes/SailfishAttribute.cs`

**Changes:**
```csharp
public class SailfishAttribute : Attribute
{
    // Existing properties...
    
    // NEW: Adaptive Sampling Attribute Properties
    public bool UseAdaptiveSampling { get; set; } = false;
    public double TargetCoefficientOfVariation { get; set; } = 0.05;
    public int MaximumSampleSize { get; set; } = 1000;
}
```

### **Step 2: Create Statistical Convergence Detector (Day 1, Afternoon)**

#### **2.1 Create IStatisticalConvergenceDetector Interface**

**File:** `source/Sailfish/Analysis/IStatisticalConvergenceDetector.cs` (NEW)

```csharp
using System.Collections.Generic;

namespace Sailfish.Analysis;

public interface IStatisticalConvergenceDetector
{
    ConvergenceResult CheckConvergence(
        IReadOnlyList<double> samples,
        double targetCoefficientOfVariation,
        double confidenceLevel,
        int minimumSampleSize);
}

public class ConvergenceResult
{
    public bool HasConverged { get; init; }
    public double CurrentCoefficientOfVariation { get; init; }
    public double CurrentMean { get; init; }
    public double CurrentStandardDeviation { get; init; }
    public int SampleCount { get; init; }
    public string Reason { get; init; } = string.Empty;
}
```

#### **2.2 Implement StatisticalConvergenceDetector**

**File:** `source/Sailfish/Analysis/StatisticalConvergenceDetector.cs` (NEW)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace Sailfish.Analysis;

public class StatisticalConvergenceDetector : IStatisticalConvergenceDetector
{
    public ConvergenceResult CheckConvergence(
        IReadOnlyList<double> samples,
        double targetCoefficientOfVariation,
        double confidenceLevel,
        int minimumSampleSize)
    {
        if (samples.Count < minimumSampleSize)
        {
            return new ConvergenceResult
            {
                HasConverged = false,
                SampleCount = samples.Count,
                Reason = $"Insufficient samples: {samples.Count} < {minimumSampleSize}"
            };
        }

        var mean = samples.Mean();
        var standardDeviation = samples.StandardDeviation();
        var coefficientOfVariation = standardDeviation / mean;

        var hasConverged = coefficientOfVariation <= targetCoefficientOfVariation;

        return new ConvergenceResult
        {
            HasConverged = hasConverged,
            CurrentCoefficientOfVariation = coefficientOfVariation,
            CurrentMean = mean,
            CurrentStandardDeviation = standardDeviation,
            SampleCount = samples.Count,
            Reason = hasConverged 
                ? $"Converged: CV {coefficientOfVariation:F4} <= target {targetCoefficientOfVariation:F4}"
                : $"Not converged: CV {coefficientOfVariation:F4} > target {targetCoefficientOfVariation:F4}"
        };
    }
}
```

### **Step 3: Create Adaptive Iteration Strategy (Day 2, Morning)**

#### **3.1 Create IIterationStrategy Interface**

**File:** `source/Sailfish/Execution/IIterationStrategy.cs` (NEW)

```csharp
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

public interface IIterationStrategy
{
    Task<IterationResult> ExecuteIterations(
        TestInstanceContainer testInstanceContainer,
        IExecutionSettings executionSettings,
        CancellationToken cancellationToken);
}

public class IterationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public int TotalIterations { get; init; }
    public bool ConvergedEarly { get; init; }
    public string? ConvergenceReason { get; init; }
}
```

#### **3.2 Implement FixedIterationStrategy (Existing Behavior)**

**File:** `source/Sailfish/Execution/FixedIterationStrategy.cs` (NEW)

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;

namespace Sailfish.Execution;

public class FixedIterationStrategy : IIterationStrategy
{
    private readonly ILogger logger;

    public FixedIterationStrategy(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task<IterationResult> ExecuteIterations(
        TestInstanceContainer testInstanceContainer,
        IExecutionSettings executionSettings,
        CancellationToken cancellationToken)
    {
        var iterations = executionSettings.SampleSize;
        
        for (var i = 0; i < iterations; i++)
        {
            logger.Log(LogLevel.Information, 
                "      ---- iteration {CurrentIteration} of {TotalIterations}", 
                i + 1, iterations);

            try
            {
                await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken);
                await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken);
                await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken);
            }
            catch (Exception ex)
            {
                return new IterationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    TotalIterations = i
                };
            }
        }

        return new IterationResult
        {
            IsSuccess = true,
            TotalIterations = iterations,
            ConvergedEarly = false
        };
    }
}
```

#### **3.3 Implement AdaptiveIterationStrategy**

**File:** `source/Sailfish/Execution/AdaptiveIterationStrategy.cs` (NEW)

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;

namespace Sailfish.Execution;

public class AdaptiveIterationStrategy : IIterationStrategy
{
    private readonly ILogger logger;
    private readonly IStatisticalConvergenceDetector convergenceDetector;

    public AdaptiveIterationStrategy(
        ILogger logger,
        IStatisticalConvergenceDetector convergenceDetector)
    {
        this.logger = logger;
        this.convergenceDetector = convergenceDetector;
    }

    public async Task<IterationResult> ExecuteIterations(
        TestInstanceContainer testInstanceContainer,
        IExecutionSettings executionSettings,
        CancellationToken cancellationToken)
    {
        var minIterations = executionSettings.MinimumSampleSize;
        var maxIterations = executionSettings.MaximumSampleSize;
        var targetCV = executionSettings.TargetCoefficientOfVariation;
        var confidenceLevel = executionSettings.ConfidenceLevel;

        var iteration = 0;
        ConvergenceResult? convergenceResult = null;

        // Execute minimum iterations first
        for (iteration = 0; iteration < minIterations; iteration++)
        {
            logger.Log(LogLevel.Information,
                "      ---- iteration {CurrentIteration} (minimum phase)",
                iteration + 1);

            try
            {
                await ExecuteSingleIteration(testInstanceContainer, cancellationToken);
            }
            catch (Exception ex)
            {
                return new IterationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    TotalIterations = iteration
                };
            }
        }

        // Check convergence after each additional iteration
        while (iteration < maxIterations)
        {
            // Get current performance data
            var currentSamples = GetCurrentSamples(testInstanceContainer);

            // Check convergence
            convergenceResult = convergenceDetector.CheckConvergence(
                currentSamples, targetCV, confidenceLevel, minIterations);

            if (convergenceResult.HasConverged)
            {
                logger.Log(LogLevel.Information,
                    "      ---- Converged after {TotalIterations} iterations: {Reason}",
                    iteration, convergenceResult.Reason);
                break;
            }

            // Execute another iteration
            logger.Log(LogLevel.Information,
                "      ---- iteration {CurrentIteration} (CV: {CurrentCV:F4}, target: {TargetCV:F4})",
                iteration + 1, convergenceResult.CurrentCoefficientOfVariation, targetCV);

            try
            {
                await ExecuteSingleIteration(testInstanceContainer, cancellationToken);
                iteration++;
            }
            catch (Exception ex)
            {
                return new IterationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    TotalIterations = iteration
                };
            }
        }

        var convergedEarly = convergenceResult?.HasConverged == true && iteration < maxIterations;

        if (!convergedEarly && iteration >= maxIterations)
        {
            logger.Log(LogLevel.Warning,
                "      ---- Reached maximum iterations ({MaxIterations}) without convergence. CV: {CurrentCV:F4}",
                maxIterations, convergenceResult?.CurrentCoefficientOfVariation ?? 0);
        }

        return new IterationResult
        {
            IsSuccess = true,
            TotalIterations = iteration,
            ConvergedEarly = convergedEarly,
            ConvergenceReason = convergenceResult?.Reason
        };
    }

    private async Task ExecuteSingleIteration(
        TestInstanceContainer testInstanceContainer,
        CancellationToken cancellationToken)
    {
        await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken);
        await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken);
        await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken);
    }

    private double[] GetCurrentSamples(TestInstanceContainer testInstanceContainer)
    {
        var timer = testInstanceContainer.CoreInvoker.GetPerformanceResults();
        return timer.ExecutionIterationPerformances
            .Select(x => (double)x.GetDurationFromTicks().TotalNanoseconds)
            .ToArray();
    }
}
```

### **Step 4: Modify TestCaseIterator (Day 2, Afternoon)**

#### **4.1 Update TestCaseIterator Constructor**

**File:** `source/Sailfish/Execution/TestCaseIterator.cs`

**Changes to constructor:**
```csharp
internal class TestCaseIterator : ITestCaseIterator
{
    private readonly ILogger logger;
    private readonly IRunSettings runSettings;
    private readonly IIterationStrategy fixedIterationStrategy;
    private readonly IIterationStrategy adaptiveIterationStrategy;

    public TestCaseIterator(
        IRunSettings runSettings,
        ILogger logger,
        IIterationStrategy fixedIterationStrategy,
        IIterationStrategy adaptiveIterationStrategy)
    {
        this.logger = logger;
        this.runSettings = runSettings;
        this.fixedIterationStrategy = fixedIterationStrategy;
        this.adaptiveIterationStrategy = adaptiveIterationStrategy;
    }

    // ... rest of class
}
```

#### **4.2 Replace Fixed Iteration Logic**

**File:** `source/Sailfish/Execution/TestCaseIterator.cs`

**Replace the iteration loop in `Iterate` method:**

**OLD CODE (lines ~39-71):**
```csharp
var iterations = runSettings.SampleSizeOverride is not null
    ? Math.Max(runSettings.SampleSizeOverride.Value, 1)
    : testInstanceContainer.SampleSize;

testInstanceContainer.CoreInvoker.SetTestCaseStart();
for (var i = 0; i < iterations; i++)
{
    // ... iteration logic
}
testInstanceContainer.CoreInvoker.SetTestCaseStop();
```

**NEW CODE:**
```csharp
// Determine which strategy to use
var executionSettings = testInstanceContainer.ExecutionSettings;
var useAdaptive = executionSettings.UseAdaptiveSampling &&
                  !runSettings.DisableOverheadEstimation;

var strategy = useAdaptive ? adaptiveIterationStrategy : fixedIterationStrategy;

// Apply sample size override if specified
if (runSettings.SampleSizeOverride.HasValue)
{
    if (useAdaptive)
    {
        executionSettings.MaximumSampleSize = Math.Max(runSettings.SampleSizeOverride.Value,
                                                      executionSettings.MinimumSampleSize);
    }
    else
    {
        executionSettings.SampleSize = Math.Max(runSettings.SampleSizeOverride.Value, 1);
    }
}

testInstanceContainer.CoreInvoker.SetTestCaseStart();

var iterationResult = await strategy.ExecuteIterations(
    testInstanceContainer,
    executionSettings,
    cancellationToken);

testInstanceContainer.CoreInvoker.SetTestCaseStop();

if (!iterationResult.IsSuccess)
{
    return CatchAndReturn(testInstanceContainer,
        new Exception(iterationResult.ErrorMessage ?? "Iteration failed"));
}

// Log convergence information for adaptive sampling
if (useAdaptive && iterationResult.ConvergedEarly)
{
    logger.Log(LogLevel.Information,
        "      ---- Adaptive sampling completed: {Reason}",
        iterationResult.ConvergenceReason);
}
```

### **Step 5: Update Dependency Injection (Day 3, Morning)**

#### **5.1 Register New Services**

**File:** `source/Sailfish/Registration/SailfishModuleRegistrations.cs`

**Add to `Load` method:**
```csharp
// Register statistical convergence detector
builder.RegisterType<StatisticalConvergenceDetector>()
    .As<IStatisticalConvergenceDetector>()
    .SingleInstance();

// Register iteration strategies
builder.RegisterType<FixedIterationStrategy>()
    .As<IIterationStrategy>()
    .Named<IIterationStrategy>("Fixed")
    .SingleInstance();

builder.RegisterType<AdaptiveIterationStrategy>()
    .As<IIterationStrategy>()
    .Named<IIterationStrategy>("Adaptive")
    .SingleInstance();
```

#### **5.2 Update TestCaseIterator Registration**

**File:** `source/Sailfish/Registration/SailfishModuleRegistrations.cs`

**Modify TestCaseIterator registration:**
```csharp
builder.Register(c => new TestCaseIterator(
    c.Resolve<IRunSettings>(),
    c.Resolve<ILogger>(),
    c.ResolveNamed<IIterationStrategy>("Fixed"),
    c.ResolveNamed<IIterationStrategy>("Adaptive")))
    .As<ITestCaseIterator>()
    .SingleInstance();
```

### **Step 6: Update Configuration Loading (Day 3, Afternoon)**

#### **6.1 Update Attribute Processing**

**File:** `source/Sailfish/Extensions/Methods/InvocationReflectionExtensionMethods.cs`

**Add method to extract adaptive sampling settings:**
```csharp
internal static IExecutionSettings RetrieveExecutionTestSettings(
    this Type type,
    int? sampleSizeOverride = null,
    int? numWarmupIterationsOverride = null)
{
    var sailfishAttribute = type.GetCustomAttributes(true)
        .OfType<SailfishAttribute>()
        .Single();

    return new ExecutionSettings(
        sailfishAttribute.AsCsv,
        sailfishAttribute.AsConsole,
        sailfishAttribute.AsMarkdown,
        sampleSizeOverride ?? sailfishAttribute.SampleSize,
        numWarmupIterationsOverride ?? sailfishAttribute.NumWarmupIterations)
    {
        DisableOverheadEstimation = sailfishAttribute.DisableOverheadEstimation,

        // NEW: Adaptive sampling settings
        UseAdaptiveSampling = sailfishAttribute.UseAdaptiveSampling,
        TargetCoefficientOfVariation = sailfishAttribute.TargetCoefficientOfVariation,
        MaximumSampleSize = sailfishAttribute.MaximumSampleSize,
        MinimumSampleSize = 10, // Default minimum
        ConfidenceLevel = 0.95 // Default confidence level
    };
}
```

### **Step 7: Testing Strategy (Day 4)**

#### **7.1 Create Unit Tests**

**File:** `source/Tests/Unit/Analysis/StatisticalConvergenceDetectorTests.cs` (NEW)

```csharp
using System.Linq;
using FluentAssertions;
using Sailfish.Analysis;
using Xunit;

namespace Tests.Unit.Analysis;

public class StatisticalConvergenceDetectorTests
{
    private readonly StatisticalConvergenceDetector detector = new();

    [Fact]
    public void CheckConvergence_WithInsufficientSamples_ReturnsFalse()
    {
        // Arrange
        var samples = new[] { 1.0, 2.0, 3.0 };

        // Act
        var result = detector.CheckConvergence(samples, 0.05, 0.95, 10);

        // Assert
        result.HasConverged.Should().BeFalse();
        result.Reason.Should().Contain("Insufficient samples");
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
        result.HasConverged.Should().BeTrue();
        result.CurrentCoefficientOfVariation.Should().BeLessThan(0.05);
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
        result.HasConverged.Should().BeFalse();
        result.CurrentCoefficientOfVariation.Should().BeGreaterThan(0.05);
    }
}
```

#### **7.2 Create Integration Tests**

**File:** `source/Tests/Integration/AdaptiveSamplingIntegrationTests.cs` (NEW)

```csharp
using System.Threading.Tasks;
using FluentAssertions;
using Sailfish.Attributes;
using Xunit;

namespace Tests.Integration;

public class AdaptiveSamplingIntegrationTests
{
    [Fact]
    public async Task AdaptiveSampling_WithLowVariabilityMethod_ConvergesEarly()
    {
        // This test would run a simple test class with adaptive sampling enabled
        // and verify that it converges before reaching maximum iterations

        // Implementation would use the existing test infrastructure
        // to run a controlled test and verify convergence behavior
    }
}

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

[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 100)]
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
```

### **Step 8: Documentation and Examples (Day 5)**

#### **8.1 Update README Examples**

**File:** `README.md`

**Add adaptive sampling example:**
```markdown
### **Adaptive Sampling**

Let Sailfish automatically determine when enough samples have been collected:

```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05)]
public class AdaptivePerformanceTest
{
    [SailfishMethod]
    public async Task MyMethod()
    {
        // Sailfish will continue sampling until the coefficient of variation
        // is below 5%, ensuring statistically reliable results
        await SomeOperation();
    }
}
```

**Configuration Options:**
- `UseAdaptiveSampling`: Enable adaptive sampling (default: false)
- `TargetCoefficientOfVariation`: Target CV for convergence (default: 0.05)
- `MaximumSampleSize`: Maximum iterations to prevent infinite loops (default: 1000)
```

#### **8.2 Create Migration Guide**

**File:** `ADAPTIVE_SAMPLING_MIGRATION_GUIDE.md` (NEW)

```markdown
# Adaptive Sampling Migration Guide

## Overview
Adaptive sampling allows Sailfish to automatically determine when sufficient samples have been collected for reliable statistical analysis, rather than using fixed sample sizes.

## Enabling Adaptive Sampling

### Option 1: Attribute-based (Recommended)
```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05)]
public class MyTest
{
    [SailfishMethod]
    public void MyMethod() { /* ... */ }
}
```

### Option 2: Global Configuration
```csharp
var settings = RunSettingsBuilder
    .CreateBuilder()
    .WithGlobalAdaptiveSampling(targetCV: 0.05, maxSamples: 500)
    .Build();
```

## Backward Compatibility
- Existing tests continue to work unchanged
- Fixed sample sizes are still supported
- Adaptive sampling is opt-in only

## Best Practices
- Start with CV target of 0.05 (5%)
- Set reasonable maximum sample sizes
- Use fixed sampling for micro-benchmarks
- Monitor convergence in test output
```

## 🎯 **Implementation Checklist**

### **Day 1: Configuration & Analysis**
- [ ] Add adaptive sampling properties to `IExecutionSettings`
- [ ] Update `ExecutionSettings` implementation
- [ ] Add attribute support to `SailfishAttribute`
- [ ] Create `IStatisticalConvergenceDetector` interface
- [ ] Implement `StatisticalConvergenceDetector`

### **Day 2: Iteration Strategies**
- [ ] Create `IIterationStrategy` interface
- [ ] Implement `FixedIterationStrategy`
- [ ] Implement `AdaptiveIterationStrategy`
- [ ] Update `TestCaseIterator` to use strategies

### **Day 3: Integration**
- [ ] Update dependency injection registrations
- [ ] Update configuration loading
- [ ] Test basic integration

### **Day 4: Testing**
- [ ] Create unit tests for convergence detector
- [ ] Create integration tests for adaptive sampling
- [ ] Test backward compatibility
- [ ] Performance testing

### **Day 5: Documentation**
- [ ] Update README with examples
- [ ] Create migration guide
- [ ] Update API documentation
- [ ] Create example test classes

## 🚨 **Critical Success Factors**

1. **Maintain Backward Compatibility**: Existing tests must continue to work unchanged
2. **Robust Error Handling**: Prevent infinite loops and handle edge cases
3. **Clear Logging**: Users should understand what's happening during adaptive sampling
4. **Performance**: Convergence checking should not significantly impact test execution
5. **Statistical Validity**: Ensure minimum sample sizes for reliable statistics

## 📊 **Expected Outcomes**

- **Improved Reliability**: Tests automatically achieve statistical significance
- **Reduced Test Time**: Fast-converging tests finish early
- **Better User Experience**: No more guessing optimal sample sizes
- **Maintained Performance**: Minimal overhead for convergence checking
```
