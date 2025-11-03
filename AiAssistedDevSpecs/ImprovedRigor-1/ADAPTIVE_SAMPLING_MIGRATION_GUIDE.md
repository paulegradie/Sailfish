# Adaptive Sampling Migration Guide

This guide explains how to adopt Sailfish Adaptive Sampling in existing projects, what changed, and how to enable/disable it.

## What is Adaptive Sampling?
Adaptive sampling automatically stops collecting samples once results are statistically stable, instead of using a fixed sample size. Stability is determined by:
- Coefficient of Variation (CV) ≤ threshold (default 5%)
- Relative Confidence Interval width ≤ threshold (default 20%) at a given confidence level (default 95%)

These criteria are evaluated after a minimum number of samples (default 10) and up to a maximum (default 1000 or as configured).

## Backward Compatibility
- No changes are required to existing tests. If you do nothing, fixed sampling is used as before.
- Adaptive sampling is opt-in:
  - Per class via `[Sailfish(UseAdaptiveSampling = true, ...)]`
  - Or globally via `RunSettingsBuilder.WithGlobalAdaptiveSampling(...)`
- Existing `SampleSize` and `NumWarmupIterations` attributes continue to work.
- If `RunSettings.SampleSizeOverride` is provided in an adaptive run, it is treated as the maximum sample size.

## How to Enable

### Per-class (attribute-based)
```csharp
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 1000)]
public class StableTiming
{
    [SailfishMethod]
    public async Task Work() => await Task.Delay(10);
}
```

### Globally (all tests in a run)
```csharp
var runSettings = RunSettingsBuilder.CreateBuilder()
    .WithGlobalAdaptiveSampling(targetCoefficientOfVariation: 0.05, maximumSampleSize: 500)
    .Build();
```

Global settings act as defaults/overrides. You can still override per class with attributes.

## Defaults & Tunables
- MinimumSampleSize: 10
- MaximumSampleSize: 1000 (safety cap)
- TargetCoefficientOfVariation: 0.05 (5%)
- ConfidenceLevel: 0.95 (95%)
- MaxConfidenceIntervalWidth: 0.20 (20% relative CI width)

## Notes & Best Practices
- Use a smaller target CV (e.g., 0.02) for highly stable code where you need tighter precision.
- Increase MaximumSampleSize if your workload is noisy and takes longer to converge.
- Keep MinimumSampleSize ≥ 10 to ensure meaningful CI estimates.
- In CI: prefer attribute-based configuration per test class for clarity; use global defaults when you want a consistent convergence policy across the suite.

## Internals / Plumbing (for contributors)
- Strategy selection occurs in `TestCaseIterator`; adaptive vs fixed iteration strategies are injected via DI.
- Convergence evaluation is implemented in `StatisticalConvergenceDetector` using CV and CI width.
- Execution settings are assembled from attributes plus global overrides via `ExecutionExtensionMethods.RetrieveExecutionTestSettings(...)`.
- Global adaptive overrides flow from `RunSettingsBuilder.WithGlobalAdaptiveSampling(...)` → `IRunSettings` → `RetrieveExecutionTestSettings(...)`.

## Troubleshooting
- If convergence is never reached, ensure `MaximumSampleSize` is high enough and thresholds are reasonable for your workload.
- For very fast tests, ensure overhead estimation is disabled or thresholds are relaxed to avoid noise from timing resolution.
- If results look suspiciously precise, inspect outliers and variance; extremely low variance may indicate a mocked or cached path.

