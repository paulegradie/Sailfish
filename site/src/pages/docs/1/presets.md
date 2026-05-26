---
title: Configuration Recipes
---

Sailfish exposes a handful of knobs that, together, decide how much data you collect before declaring a result stable. This page gives you three pre-tuned recipes you can copy as a starting point: a sensible default, a tighter profile for release gates, and a looser profile for noisy CI hosts.

{% callout title="TL;DR" type="note" %}
- The three knobs that matter most are `TargetCoefficientOfVariation`, `MaxConfidenceIntervalWidth`, and `MinimumSampleSize`. `MaximumSampleSize` caps total work.
- Tighter (lower CV / lower CI width / higher min N) → catches smaller regressions, runs longer.
- Looser (higher CV / wider CI width / lower min N) → finishes sooner, may miss small regressions.
- These are recipes, not an API. There is no `Preset` enum today — copy the values that fit your environment.
{% /callout %}

## Recipes

| Recipe | When to use | `TargetCoefficientOfVariation` | `MaxConfidenceIntervalWidth` | `MinimumSampleSize` | `MaximumSampleSize` |
| --- | --- | --- | --- | --- | --- |
| **Default** | Most local and CI runs. Matches Sailfish's built-in defaults. | `0.05` (5%) | `0.20` (20%) | `10` | `1000` |
| **Tight** | Release gates and baseline verification where small regressions matter. | `0.03` (3%) | `0.12` (12%) | `50` | `2000` |
| **Relaxed** | Shared/noisy CI hosts where predictable completion time matters more than micro-changes. | `0.10` (10%) | `0.30` (30%) | `10` | `1000` |

The **Default** row is what you get from a bare `[Sailfish]` attribute today — see [SailfishAttribute defaults](https://github.com/paulegradie/Sailfish/blob/main/source/Sailfish/Attributes/SailfishAttribute.cs).

{% callout title="Sailfish already auto-tunes for fast methods" type="note" %}
When `UseAdaptiveSampling = true`, the [Adaptive Sampling](/docs/1/adaptive-sampling) selector inspects a short pilot and *automatically* tightens CV / CI / min-N for fast methods (e.g., UltraFast → CV `0.03`, CI `0.12`, min `50`). You generally don't need a "Tight" recipe just to catch fast-method regressions — you need it when you want all methods to meet that bar.
{% /callout %}

## Apply globally (recommended)

Set one policy for the whole run with `RunSettingsBuilder`:

```csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithGlobalAdaptiveSampling(
        targetCoefficientOfVariation: 0.05,
        maximumSampleSize: 1000)
    .Build();
```

{% callout title="Limitation" type="note" %}
`WithGlobalAdaptiveSampling` currently exposes only `TargetCoefficientOfVariation` and `MaximumSampleSize` globally. To pin `MinimumSampleSize` and `MaxConfidenceIntervalWidth` across a run today, set them on each `[Sailfish]` attribute (or via a shared base class). A richer global API is tracked in the preset-builder work.
{% /callout %}

## Apply per class (attribute)

```csharp
// Default
[Sailfish(
    UseAdaptiveSampling = true,
    MinimumSampleSize = 10,
    TargetCoefficientOfVariation = 0.05,
    MaxConfidenceIntervalWidth = 0.20,
    MaximumSampleSize = 1000)]
public class DefaultBenchmarks { }

// Tight — release gates, low noise
[Sailfish(
    UseAdaptiveSampling = true,
    MinimumSampleSize = 50,
    TargetCoefficientOfVariation = 0.03,
    MaxConfidenceIntervalWidth = 0.12,
    MaximumSampleSize = 2000)]
public class TightGateBenchmarks { }

// Relaxed — noisy CI hosts
[Sailfish(
    UseAdaptiveSampling = true,
    MinimumSampleSize = 10,
    TargetCoefficientOfVariation = 0.10,
    MaxConfidenceIntervalWidth = 0.30,
    MaximumSampleSize = 1000)]
public class RelaxedCiBenchmarks { }
```

## What about outliers?

`OutlierStrategy` is orthogonal to the recipes above but matters at least as much on noisy hosts. The shipping default preserves legacy behavior (`RemoveAll`); most users on shared CI runners are better off with `RemoveUpper` or `Adaptive`. See [Outlier Handling](/docs/1/outlier-handling) for the full picture and the global builder hook.

## Where the numbers come from

- **Default** matches the built-in defaults on `[Sailfish]` (`TargetCoefficientOfVariation = 0.05`, `MaxConfidenceIntervalWidth = 0.20`, `MinimumSampleSize = 10`, `MaximumSampleSize = 1000`).
- **Tight** mirrors the values that [`AdaptiveParameterSelector`](https://github.com/paulegradie/Sailfish/blob/main/source/Sailfish/Analysis/AdaptiveParameterSelector.cs) recommends for UltraFast methods (`0.03` CV, `0.12` CI, min `50`) — applying them globally extends that bar to slower methods too.
- **Relaxed** mirrors the VerySlow-category recommendation (`0.10` CV, `0.30` CI, min `10`), which is the lowest the adaptive selector will go on its own.

## Troubleshooting

- **"My tests never converge."** You're likely on a noisy host with a tight recipe. Move to **Relaxed**, or keep CV/CI tight and raise `MaximumSampleSize`.
- **"My CI run takes forever."** Lower `MaximumSampleSize`, switch to **Relaxed**, or enable [Outlier Handling](/docs/1/outlier-handling) with `RemoveUpper`.
- **"I'm missing real regressions."** Move to **Tight**, and verify [Environment Health](/docs/1/environment-health) isn't flagging the host as unstable.
- **"Results vary between runs on the same machine."** Tighten `TargetCoefficientOfVariation` and raise `MinimumSampleSize`; consider warming up more iterations via `NumWarmupIterations`.
