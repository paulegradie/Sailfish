# Sailfish Adaptive Sampling Demo Tests

This directory contains demonstration test classes that showcase the new adaptive sampling feature in Sailfish. These tests are designed to illustrate how adaptive sampling works with different performance patterns and configurations.

## 🎯 What is Adaptive Sampling?

Adaptive sampling is a new feature in Sailfish that automatically determines when sufficient performance samples have been collected for reliable statistical analysis. Instead of running a fixed number of iterations, adaptive sampling continues until the coefficient of variation (CV) falls below a target threshold, indicating statistical convergence.

## 📊 Demo Test Classes

### 1. ConsistentPerformanceDemo
**Purpose**: Demonstrates adaptive sampling with low-variability operations.
**Expected Behavior**: Should converge quickly (typically 10-20 iterations) due to consistent timing.

```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 100)]
public class ConsistentPerformanceDemo
```

**Key Features**:
- `ConsistentOperation`: Always takes ~10ms, should converge very quickly
- `SlightlyVariableOperation`: 9-11ms range, still converges relatively quickly

### 2. VariablePerformanceDemo
**Purpose**: Demonstrates adaptive sampling with high-variability operations.
**Expected Behavior**: Should reach maximum iterations (50) without converging due to high variability.

```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 50)]
public class VariablePerformanceDemo
```

**Key Features**:
- `HighlyVariableOperation`: 0-50ms range with predictable pattern
- `RandomVariableOperation`: 1-100ms random range, very high variability

### 3. FixedSamplingComparison
**Purpose**: Provides a baseline comparison using traditional fixed sampling.
**Expected Behavior**: Always runs exactly 20 iterations regardless of variability.

```csharp
[Sailfish(UseAdaptiveSampling = false, SampleSize = 20)]
public class FixedSamplingComparison
```

### 4. StrictConvergenceDemo
**Purpose**: Shows adaptive sampling with stricter convergence criteria.
**Expected Behavior**: Requires more iterations to achieve 2% CV instead of 5%.

```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.02, MaximumSampleSize = 200)]
public class StrictConvergenceDemo
```

### 5. WorkloadPatternsDemo
**Purpose**: Demonstrates adaptive sampling with different types of workloads.
**Expected Behavior**: Shows how adaptive sampling handles CPU, I/O, and memory allocation patterns.

**Key Features**:
- `CpuIntensiveOperation`: Consistent CPU-bound work
- `IoSimulationOperation`: I/O simulation with slight variability
- `MemoryAllocationOperation`: Memory allocation with GC-induced variability

### 6. EdgeCasesDemo
**Purpose**: Tests adaptive sampling behavior with edge cases and extreme scenarios.
**Expected Behavior**: Handles very fast operations and occasional performance spikes.

**Key Features**:
- `VeryFastOperation`: Microsecond-level timing tests
- `ZeroDelayOperation`: Essentially no delay
- `OccasionalSpike`: Usually fast with 5% chance of 50ms spike

## 🚀 Running the Demo Tests

### Prerequisites
- .NET 8 or later
- Sailfish performance testing framework

### Running All Demo Tests
```bash
dotnet test --filter "FullyQualifiedName~AdaptiveSamplingDemos"
```

### Running Specific Demo Classes
```bash
# Run only consistent performance demos
dotnet test --filter "FullyQualifiedName~ConsistentPerformanceDemo"

# Run only variable performance demos
dotnet test --filter "FullyQualifiedName~VariablePerformanceDemo"

# Run comparison between adaptive and fixed sampling
dotnet test --filter "FullyQualifiedName~FixedSamplingComparison"
```

## 📈 Understanding the Results

### Adaptive Sampling Output
When running adaptive sampling tests, you'll see output like:
```
---- iteration 1 (minimum phase)
---- iteration 2 (minimum phase)
...
---- iteration 10 (minimum phase)
---- iteration 11 (CV: 0.0823, target: 0.0500)
---- iteration 12 (CV: 0.0654, target: 0.0500)
---- iteration 13 (CV: 0.0487, target: 0.0500)
---- Converged after 13 iterations: Converged: CV 0.0487 <= target 0.0500
---- Adaptive sampling completed: Converged: CV 0.0487 <= target 0.0500
```

### Key Metrics to Watch
- **Total Iterations**: How many samples were needed
- **Coefficient of Variation (CV)**: Lower values indicate more consistent performance
- **Convergence Status**: Whether the test converged or reached maximum iterations

## ⚙️ Configuration Options

### Adaptive Sampling Attributes
```csharp
[Sailfish(
    UseAdaptiveSampling = true,           // Enable adaptive sampling
    TargetCoefficientOfVariation = 0.05,  // 5% CV threshold (lower = stricter)
    MaximumSampleSize = 1000              // Safety limit to prevent infinite loops
)]
```

### Default Values
- `UseAdaptiveSampling`: `false` (backward compatibility)
- `TargetCoefficientOfVariation`: `0.05` (5%)
- `MinimumSampleSize`: `10` (always run at least 10 iterations)
- `MaximumSampleSize`: `1000` (safety limit)
- `ConfidenceLevel`: `0.95` (95% confidence)

## 🔍 Interpreting Results

### When Adaptive Sampling Converges Early
- **Good**: Indicates consistent, reliable performance
- **Typical for**: Well-optimized algorithms, simple operations, stable environments

### When Adaptive Sampling Reaches Maximum
- **Normal**: For inherently variable operations
- **Consider**: Increasing `MaximumSampleSize` or relaxing `TargetCoefficientOfVariation`
- **Typical for**: I/O operations, network calls, operations with external dependencies

### Choosing Target CV
- **0.01-0.02**: Very strict, for critical performance measurements
- **0.05**: Good default for most scenarios
- **0.10**: More relaxed, for inherently variable operations

## 🎓 Best Practices

1. **Start with defaults**: Use `TargetCoefficientOfVariation = 0.05` initially
2. **Monitor convergence**: Check if tests converge or hit maximum iterations
3. **Adjust based on workload**: Stricter CV for consistent operations, relaxed for variable ones
4. **Use fixed sampling for comparison**: Compare adaptive results with fixed sampling baselines
5. **Consider test environment**: Shared CI environments may need relaxed thresholds

## 🔧 Troubleshooting

### Tests Never Converge
- Increase `MaximumSampleSize`
- Relax `TargetCoefficientOfVariation` (e.g., 0.05 → 0.10)
- Check for external factors causing variability

### Tests Converge Too Quickly
- Decrease `TargetCoefficientOfVariation` (e.g., 0.05 → 0.02)
- Increase `MinimumSampleSize` if needed
- Verify the operation is actually being measured correctly

### Inconsistent Results
- Check test environment stability
- Consider using fixed sampling for baseline comparison
- Review test implementation for unintended variability sources
