---
title: Presets
---

Presets give you a fast way to choose a measurement policy based on where you are running tests and how much noise you can tolerate.

{% callout title="Sensitivity vs Stability" type="note" %}
- **More sensitivity** catches smaller regressions, but needs tighter CV/CI targets and typically more samples.
- **More stability/speed** finishes faster in noisy environments, but may miss very small performance changes.
{% /callout %}

## Preset matrix

| Preset | Intended environment | CV target | Max relative CI width | Minimum sample size | Statistical test type | Effect-size threshold |
| --- | --- | --- | --- | --- | --- | --- |
| **Balanced (default)** | General local + CI runs where you want reliable signal without long runtime | `0.05` (5%) | `0.20` (20%) | `10` | `TwoSampleWilcoxonSignedRankTest` | `~2%` ratio change (guideline) |
| **Sensitive** | Baseline verification and release/perf gates where catching small regressions matters most | `0.03` (3%) | `0.12` (12%) | `50` | `TwoSampleWilcoxonSignedRankTest` | `~1%` ratio change (guideline) |
| **Stable/Fast** | Busy CI, shared runners, or noisy hosts where predictable completion time is more important than micro-changes | `0.10` (10%) | `0.30` (30%) | `10` | `TwoSampleWilcoxonSignedRankTest` | `~5%` ratio change (guideline) |

### Where these numbers come from

- Sailfish adaptive defaults are CV `0.05`, CI width `0.20`, and minimum sample size `10`.
- Adaptive speed-category tuning supports tighter values for very fast methods (`0.03` CV, `0.12` CI, min `50`) and looser values for very slow methods (`0.10` CV, `0.30` CI, min `10`).
- SailDiff defaults to `TwoSampleWilcoxonSignedRankTest`, which is robust for many real-world perf distributions.

{% callout title="Preset tradeoff" type="note" %}
If you are unsure, start with **Balanced**. Move to **Sensitive** when regression risk is high, and move to **Stable/Fast** when CI time budgets or host noise dominate.
{% /callout %}

## Example configuration snippets

### Balanced
```csharp
[Sailfish(
    UseAdaptiveSampling = true,
    MinimumSampleSize = 10,
    TargetCoefficientOfVariation = 0.05,
    MaxConfidenceIntervalWidth = 0.20
)]
public class BalancedBenchmarks { }
```

### Sensitive
```csharp
[Sailfish(
    UseAdaptiveSampling = true,
    MinimumSampleSize = 50,
    TargetCoefficientOfVariation = 0.03,
    MaxConfidenceIntervalWidth = 0.12
)]
public class SensitiveBenchmarks { }
```

### Stable/Fast
```csharp
[Sailfish(
    UseAdaptiveSampling = true,
    MinimumSampleSize = 10,
    TargetCoefficientOfVariation = 0.10,
    MaxConfidenceIntervalWidth = 0.30
)]
public class FastCiBenchmarks { }
```
