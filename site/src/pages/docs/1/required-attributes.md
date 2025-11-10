---
title: Required Attributes
---

## Sailfish Attribute

Apply the `[Sailfish]` attribute to the class.

### Basic Usage (Fixed Sampling)

```csharp
[Sailfish(
    SampleSize = 45,
    NumWarmupIterations = 2,
    DisableOverheadEstimation = false,
    Disabled = false)]
public class AMostBasicTest { ... }
```

### Adaptive Sampling Usage

```csharp
[Sailfish(
    UseAdaptiveSampling = true,
    TargetCoefficientOfVariation = 0.05,
    MaximumSampleSize = 1000,
    NumWarmupIterations = 2)]
public class AdaptiveTest { ... }
```

---

## Fixed Sampling Parameters

These parameters control traditional fixed-sample-size testing.

#### SampleSize

Sets the number of times the SailfishMethod will be invoked. This number should be as high as you can tolerate. Larger sample size improves the quality of the result.

**Default:** 20
**Range:** 2 to int.MaxValue
**Note:** Ignored when `UseAdaptiveSampling = true`

#### NumWarmupIterations

Sets the number of times the SailfishMethod will be invoked before timing begins. This includes invocation of the SailfishIterationSetup and SailfishIterationTeardown lifecycle methods.

**Default:** 2
**Range:** 0 to int.MaxValue
**Note:** Applies to both fixed and adaptive sampling

---

## Adaptive Sampling Parameters

These parameters enable and control adaptive sampling, which automatically stops collecting samples when results are statistically stable.

{% callout title="Learn More" type="note" %}
For detailed information about adaptive sampling, see the [Adaptive Sampling](/docs/1/adaptive-sampling) documentation.
{% /callout %}

#### UseAdaptiveSampling

Enables adaptive sampling for this test class. When enabled, tests will continue until statistical convergence is achieved rather than running a fixed number of iterations.

**Default:** `false`
**Type:** `bool`

```csharp
[Sailfish(UseAdaptiveSampling = true)]
public class MyTest { ... }
```

#### TargetCoefficientOfVariation

Sets the target coefficient of variation (CV) threshold for convergence detection. The CV is calculated as (standard deviation / mean). Lower values require more statistical precision and may result in more samples being collected.

**Default:** `0.05` (5%)
**Type:** `double`
**Typical Range:** 0.01 to 0.10

```csharp
// Stricter convergence (more samples, higher precision)
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.02)]
public class HighPrecisionTest { ... }

// Relaxed convergence (fewer samples, faster tests)
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.10)]
public class QuickTest { ... }
```

**When to adjust:**
- **Lower (0.01-0.02):** For highly stable microbenchmarks requiring maximum precision
- **Default (0.05):** Good balance for most scenarios
- **Higher (0.08-0.10):** For inherently noisy operations or when speed is prioritized over precision

#### MaximumSampleSize

Sets the maximum number of samples to collect when using adaptive sampling. This acts as a safety cap to prevent infinite loops in case of non-converging tests.

**Default:** `1000`
**Type:** `int`
**Range:** Must be greater than MinimumSampleSize (10)

```csharp
// Lower cap for faster CI runs
[Sailfish(UseAdaptiveSampling = true, MaximumSampleSize = 500)]
public class CITest { ... }

// Higher cap for thorough analysis
[Sailfish(UseAdaptiveSampling = true, MaximumSampleSize = 2000)]
public class DetailedAnalysis { ... }
```

**When to adjust:**
- **Lower (100-500):** For CI environments where time is critical
- **Default (1000):** Sufficient for most scenarios
- **Higher (1500-3000):** For noisy workloads that need more samples to converge

{% callout title="Additional Adaptive Parameters" type="note" %}
The following adaptive sampling parameters are available via global configuration using `RunSettingsBuilder` but cannot be set per-class:
- **MinimumSampleSize:** Minimum samples before checking convergence (default: 10)
- **ConfidenceLevel:** Confidence level for CI width calculation (default: 0.95)
- **MaxConfidenceIntervalWidth:** Maximum relative CI width threshold (default: 0.20)
- **UseRelativeConfidenceInterval:** Whether to use relative CI width (default: true)

See [Adaptive Sampling](/docs/1/adaptive-sampling) for details on global configuration.
{% /callout %}

---

## General Parameters

These parameters apply to all test configurations.

#### DisableOverheadEstimation

Sailfish performs overhead executions to estimate how much test result variance is due to the underlying hardware. This will disable the feature, which will significantly increase test runtime. Estimation is performed before and after each test method is fully executed.

**Default:** `false`
**Type:** `bool`

#### Disabled

Tests are discoverable but ignored when set to true. Useful for temporarily disabling tests without removing them.

**Default:** `false`
**Type:** `bool`

---

## Time Budget & Iteration Controls

These attributes help shape iteration length and enforce optional time limits. They work with both fixed and adaptive sampling.

#### OperationsPerInvoke
Number of inner operations executed per measured iteration. Useful for microbenchmarks to amortize timer overhead.

- Default: `1`
- Type: `int`

#### TargetIterationDurationMs
Target duration (milliseconds) for a single measured iteration. Use together with `OperationsPerInvoke` to steer iteration cost.

- Default: `0` (disabled)
- Type: `int`


{% callout title="Iteration Tuning" type="note" %}
When `TargetIterationDurationMs > 0` and `OperationsPerInvoke <= 1`, Sailfish will auto‑tune the operations per measured iteration to bring each iteration near the target. If you set `OperationsPerInvoke > 1`, your explicit value is honored and tuning will not run. See [Iteration Tuning](/docs/1/iteration-tuning).
{% /callout %}

#### MaxMeasurementTimePerMethodMs
Maximum allowed wall-clock time (milliseconds) for measuring a single test method. When set (>0), it enables time-budget awareness throughout execution.

- Default: `0` (no limit)
- Type: `int`

#### UseTimeBudgetController
Enables a controller that, under tight remaining time budgets, relaxes precision targets slightly (within conservative caps) so tests complete within budget. Backward compatible: inert unless both this is `true` and `MaxMeasurementTimePerMethodMs > 0`.

- Default: `false`
- Type: `bool`

```csharp
[Sailfish(UseTimeBudgetController = true, MaxMeasurementTimePerMethodMs = 30_000)]
public class BudgetedClass { }
```

{% callout title="Learn more" type="note" %}
See the dedicated page: [Precision/Time Budget Controller](/docs/1/precision-time-budget)
{% /callout %}


## SailfishMethod Attribute

Apply the [SailfishMethod] attribute to a method you wish to time.

```csharp
[SailfishMethod]
public async Task SailfishMethod(CancellationToken ct) => ...
```
---

### Injecting Cancellation Tokens

Any Sailfish lifecycle method can request a `CancellationToken`, which will be injected by the framework.

### Sync vs Async

Any lifecycle method may be implemented as sync or async.
