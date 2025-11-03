---
title: Adaptive Sampling
---

Adaptive Sampling lets Sailfish stop collecting samples automatically once your results are statistically stable. This reduces test time while maintaining rigor, and removes the need to guess a fixed sample size up front.

{% callout title="TL;DR" type="note" %}
- Opt-in per class via [Sailfish] or globally via RunSettingsBuilder
- Converges when both criteria are met (after a minimum number of samples):
  - Coefficient of Variation (CV) ≤ threshold (default 5%)
  - Relative Confidence Interval (CI) width ≤ threshold (default 20%) at a given confidence level (default 95%)
- Caps total work with a maximum sample size
{% /callout %}

## Why it exists
Fixed sample sizes are either too small (noisy results) or too large (wasted time). Adaptive sampling balances speed and precision by collecting “just enough” data for reliable comparisons.

## How it works
After warmups, Sailfish collects iterations and evaluates convergence when there are at least `MinimumSampleSize` samples. On each check:
- Compute CV = standard deviation / mean
- Compute the relative CI width at the configured `ConfidenceLevel`
- If both are within thresholds, sampling stops; otherwise continue until `MaximumSampleSize`

See also: [Confidence Intervals](/docs/1/confidence-intervals)

## Defaults and tunables
- MinimumSampleSize: 10
- MaximumSampleSize: 1000 (safety cap)
- TargetCoefficientOfVariation: 0.05 (5%)
- ConfidenceLevel: 0.95 (95%)
- MaxConfidenceIntervalWidth: 0.20 (20% relative CI width)
- UseRelativeConfidenceInterval: true

{% callout title="Global vs Attribute precedence" type="note" %}
Global defaults from RunSettingsBuilder act as overrides/defaults. Individual [Sailfish] attributes can still set different values per class.
{% /callout %}

## How to enable

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

### Interactions with fixed settings
- `SampleSize` on the attribute still works for fixed sampling
- In an adaptive run, `SampleSizeOverride` (from run settings) is treated as the `MaximumSampleSize`
- Warmups (`NumWarmupIterations`) still apply

## When to use
Use adaptive sampling when:
- You want consistent precision across tests without hand-tuning N
- CI runtime matters and you want tests to stop early when stable
- Your benchmarks vary in noise; stable ones converge fast, noisy ones gather more data

Prefer fixed sampling when:
- You must replicate historical runs with exact N (compliance/audit)
- You’re comparing runs across time where identical N is a hard requirement

## Migration (existing projects)
Adaptive sampling is backward-compatible and opt-in.
- Do nothing: fixed sampling continues as before
- Enable per class: `UseAdaptiveSampling = true`
- Or enable globally with the builder (recommended for consistent policy)
- If using `SampleSizeOverride`, note it caps the number of adaptive samples

## Best practices
- Tighten `TargetCoefficientOfVariation` (e.g., 0.02) for highly stable microbenchmarks
- Raise `MaximumSampleSize` for noisy workloads to allow convergence
- Keep `MinimumSampleSize ≥ 10` so CI estimates are meaningful
- In CI, prefer class attributes for clarity and apply global defaults to unify policy

## Troubleshooting
- “Never converges”: lower thresholds or raise `MaximumSampleSize`; investigate outliers
- “Too precise to be true”: check for caching/mocks; extremely low variance can be misleading
- “Runs are slow”: relax thresholds or reduce `MaximumSampleSize`

## Internals (for contributors)
- Strategy selection happens in `TestCaseIterator` (adaptive vs fixed)
- Convergence detection is implemented in `StatisticalConvergenceDetector`
- Execution settings are composed from attributes plus global overrides via `ExecutionExtensionMethods.RetrieveExecutionTestSettings(...)`
- Global overrides flow from `RunSettingsBuilder.WithGlobalAdaptiveSampling(...)` → `IRunSettings` → `RetrieveExecutionTestSettings(...)`

