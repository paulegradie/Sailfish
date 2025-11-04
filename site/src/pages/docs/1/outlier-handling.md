---
title: Outlier Handling
---

Sailfish detects statistical outliers in timing data and (by default) removes them to produce stable summaries. This page explains the available strategies and how to configure them globally for a run.

{% callout title="TL;DR" type="note" %}
- Default behavior (no action needed): legacy outlier removal removes both lower and upper outliers ("RemoveAll") for backward compatibility.
- New (opt‑in): enable configurable outlier handling and choose a strategy globally via `RunSettingsBuilder.WithGlobalOutlierHandling(...)`.
- Strategies: `RemoveUpper`, `RemoveLower`, `RemoveAll`, `DontRemove`, `Adaptive`.
{% /callout %}

## Why it exists
Raw timings can contain spikes (e.g., GC, context switches) or dips (warm cache artifacts). Outlier handling helps keep your central tendency and CI calculations representative of steady‑state performance.

Internally, Sailfish uses Tukey fences (via Perfolizer) to detect outliers.

## Strategies
- **RemoveUpper**: Remove only high‑side outliers (long‑tail spikes). Good default for most production‑like workloads where occasional spikes occur.
- **RemoveLower**: Remove only low‑side outliers (rare dips). Useful when warm caches or unrealistic fast paths skew left.
- **RemoveAll**: Remove both sides. This is the historical Sailfish default.
- **DontRemove**: Keep all data points. Outliers are still detected and reported, but not removed from statistics.
- **Adaptive**: Remove outliers on the side(s) where they are actually detected.

## Defaults and compatibility
- By default, Sailfish preserves legacy behavior: outliers are removed on both sides (equivalent to `RemoveAll`).
- The configurable path is opt‑in. When you enable it without specifying a strategy, Sailfish uses `RemoveUpper` as a sensible default.

{% callout title="Backwards compatibility" type="note" %}
If you do nothing, behavior is unchanged for existing projects. Enabling global configuration affects only the run you configure.
{% /callout %}

## Configure globally (recommended)
Apply a single policy for the entire run using the builder:

```csharp
var run = RunSettingsBuilder.CreateBuilder()
    .WithGlobalOutlierHandling(useConfigurable: true, strategy: OutlierStrategy.RemoveUpper)
    .Build();
```

This sets a typed, discoverable global policy that flows to all test classes in the run. You can switch strategies (e.g., `DontRemove` for raw analysis, or `Adaptive` for side‑aware removal).

## Per‑class configuration
Attribute‑level controls for outlier strategy are not yet exposed. Use the global configuration above to set a consistent policy across tests.

## How it interacts with statistics
- Outliers that are removed do not participate in mean, standard deviation, or confidence interval calculations.
- Detected outliers may still be surfaced in reporting to aid diagnosis.
- See also: [Confidence Intervals](/docs/1/confidence-intervals) and [Adaptive Sampling](/docs/1/adaptive-sampling).

## Troubleshooting
- "Too many outliers removed": try `Adaptive` or a side‑specific strategy instead of `RemoveAll`.
- "Numbers look noisy": consider keeping `RemoveUpper` enabled to mitigate sporadic spikes.
- "I need raw, unfiltered data": use `DontRemove` (outliers will still be flagged for inspection).

