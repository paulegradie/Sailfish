---
title: Configuration Recipes
---

Sailfish exposes a handful of knobs that, together, decide how much data you collect before declaring a result stable. This page gives you three pre-tuned recipes you can apply with a single builder call: a sensible default, a tighter profile for release gates, and a looser profile for noisy CI hosts.

{% callout title="TL;DR" type="note" %}
- Use `RunSettingsBuilder.WithPreset(SailfishPreset.Default | Tight | Relaxed)` to seed the whole run.
- Presets seed adaptive sampling, outlier handling, **and** SailDiff alpha at once.
- Any explicit `WithGlobalX` / `WithSailDiff(...)` call on the builder wins over the preset.
- The three knobs that matter most are `TargetCoefficientOfVariation`, `MaxConfidenceIntervalWidth`, and `MinimumSampleSize`. `MaximumSampleSize` caps total work.
{% /callout %}

## Recipes

| Recipe | When to use | `TargetCoefficientOfVariation` | `MaxConfidenceIntervalWidth` | `MinimumSampleSize` | `MaximumSampleSize` | `OutlierStrategy` | SailDiff `alpha` |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Default** | Most local and CI runs. Matches Sailfish's built-in defaults. | `0.05` (5%) | `0.20` (20%) | `10` | `1000` | `RemoveUpper` | `0.05` |
| **Tight** | Release gates and baseline verification where small regressions matter. | `0.03` (3%) | `0.12` (12%) | `50` | `2000` | `RemoveUpper` | `0.01` |
| **Relaxed** | Shared/noisy CI hosts where predictable completion time matters more than micro-changes. | `0.10` (10%) | `0.30` (30%) | `10` | `1000` | `Adaptive` | `0.10` |

The **Default** row is what you get from a bare `[Sailfish]` attribute today.

{% callout title="Sailfish already auto-tunes for fast methods" type="note" %}
When `UseAdaptiveSampling = true`, the [Adaptive Sampling](/docs/1/adaptive-sampling) selector inspects a short pilot and *automatically* tightens CV / CI / min-N for fast methods (e.g., UltraFast → CV `0.03`, CI `0.12`, min `50`). You generally don't need a "Tight" recipe just to catch fast-method regressions — you need it when you want all methods to meet that bar.
{% /callout %}

## Apply globally (recommended)

Seed the whole run from a preset with one call:

```csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithPreset(SailfishPreset.Default) // or .Tight / .Relaxed
    .WithSailDiff()                     // presets seed SailDiff defaults, but SailDiff itself is still opt-in
    .Build();
```

Notes:

- The preset is applied at `Build()`, so any explicit `WithGlobalX` (e.g. `WithGlobalOutlierHandling`, `WithGlobalAdaptiveSampling`) or `WithSailDiff(settings)` call **wins** over the preset, regardless of call order.
- Calling `WithPreset` multiple times — last call wins (no silent divergence between execution and SailDiff settings).
- `WithPreset` does **not** enable SailDiff by itself; call `WithSailDiff()` separately if you want SailDiff to run.

## Apply per class (attribute)

If you'd rather pin the values per class, the same numbers map directly onto the attribute:

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
    MaximumSampleSize = 1000,
    UseConfigurableOutlierDetection = true,
    OutlierStrategy = OutlierStrategy.Adaptive)]
public class RelaxedCiBenchmarks { }
```

## What about outliers?

Presets already seed `OutlierStrategy` (Default/Tight → `RemoveUpper`, Relaxed → `Adaptive`). If you skip presets, the global default still falls back to the legacy `RemoveAll` path until you opt in via `UseConfigurableOutlierDetection`. See [Outlier Handling](/docs/1/outlier-handling) for the full picture.

## Where the numbers come from

- **Default** matches the built-in defaults on `[Sailfish]` (`TargetCoefficientOfVariation = 0.05`, `MaxConfidenceIntervalWidth = 0.20`, `MinimumSampleSize = 10`, `MaximumSampleSize = 1000`).
- **Tight** mirrors the values that [`AdaptiveParameterSelector`](https://github.com/paulegradie/Sailfish/blob/main/source/Sailfish/Analysis/AdaptiveParameterSelector.cs) recommends for UltraFast methods (`0.03` CV, `0.12` CI, min `50`) — applying them globally extends that bar to slower methods too.
- **Relaxed** mirrors the VerySlow-category recommendation (`0.10` CV, `0.30` CI, min `10`), which is the lowest the adaptive selector will go on its own.

## Troubleshooting

- **"My tests never converge."** You're likely on a noisy host with a tight recipe. Move to **Relaxed**, or keep CV/CI tight and raise `MaximumSampleSize`.
- **"My CI run takes forever."** Lower `MaximumSampleSize`, switch to **Relaxed**, or enable [Outlier Handling](/docs/1/outlier-handling) with `RemoveUpper`.
- **"I'm missing real regressions."** Move to **Tight**, and verify [Environment Health](/docs/1/environment-health) isn't flagging the host as unstable.
- **"Results vary between runs on the same machine."** Tighten `TargetCoefficientOfVariation` and raise `MinimumSampleSize`; consider warming up more iterations via `NumWarmupIterations`.
