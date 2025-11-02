---
title: Confidence Intervals
---

## Overview

Confidence intervals (CIs) quantify the uncertainty around an estimated mean runtime. At a 95% confidence level, if you were to repeat the same experiment many times, about 95% of the constructed intervals would contain the true mean. A 99% CI is wider (more conservative) than a 95% CI.

## Where you’ll see them
- Console/IDE: a row per CI level (e.g., "95% CI ±", "99% CI ±")
- Markdown: compact per‑test CI summary
- CSV: numeric columns CI95_MOE and CI99_MOE

Tip: In some terminals the ± symbol may render as a replacement character due to encoding. This is cosmetic only.

## How Sailfish calculates CIs
Given a set of n cleaned samples with sample standard deviation `s`:
- Standard error: `SE = s / sqrt(n)`
- Degrees of freedom: `df = n − 1`
- For each confidence level `cl` in (0, 1):
  - `t = StudentT.InvCDF(df, 0.5 + cl/2)`
  - Margin of error: `MOE = t × SE`
  - Interval: `[mean − MOE, mean + MOE]`

Edge cases: If `n ≤ 1` or `SE = 0`, the MOE is `0`.

## How to interpret them
- Frequentist view: 95% refers to the long‑run procedure, not “a 95% chance this one interval contains the true mean.”
- Larger `n` → smaller `SE` → narrower CIs.
- 99% CIs are wider than 95% (more conservative).
- Overlapping CIs don’t strictly prove “no difference,” but are a helpful visual cue.

## Defaults
- Sailfish computes and reports multiple CIs by default: 95% and 99%.
- A primary legacy confidence level remains for backward compatibility: 0.95.

## Customizing confidence levels
Execution settings expose both the primary confidence level and the set of levels that are reported:
- `ExecutionSettings.ConfidenceLevel` (double in (0, 1)): primary legacy level (default `0.95`)
- `ExecutionSettings.ReportConfidenceLevels` (`IReadOnlyList<double>`): levels used for multi‑CI reporting (default `[0.95, 0.99]`)

Notes:
- Values are fractions (e.g., `0.95` = 95%), must satisfy `0 < cl < 1`, and duplicates are ignored.
- As of now, `RunSettingsBuilder` does not expose a fluent method to set `ReportConfidenceLevels`.

Advanced/programmatic example (embedding Sailfish):

```csharp
using Sailfish.Execution;

var exec = new ExecutionSettings
{
    ConfidenceLevel = 0.90,                                      // primary legacy level
    ReportConfidenceLevels = new List<double> { 0.90, 0.95, 0.99 } // reported levels
};

// Pass 'exec' into your host/integration where ExecutionSettings is consumed
// so PerformanceRunResult computes margins at these levels.
```

