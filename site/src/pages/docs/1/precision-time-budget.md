---
title: Precision/Time Budget Controller
---

The Precision/Time Budget Controller helps long‑running tests finish within a per‑method time budget by conservatively relaxing precision targets when enabled. It integrates with Adaptive Sampling and is fully backward‑compatible (off by default).

{% callout title="TL;DR" type="note" %}
- Opt‑in per class via `[Sailfish(UseTimeBudgetController = true, MaxMeasurementTimePerMethodMs = 30000)]`
- Only active when `MaxMeasurementTimePerMethodMs > 0`
- After the pilot/minimum phase, estimates remaining budget and per‑iteration cost; if tight, relaxes CV/CI thresholds within caps
- Conservative caps (v1): CV ≤ 0.20, relative CI width ≤ 0.50; never tightens beyond current thresholds
- Emits an INFO log describing the adjustment
{% /callout %}

## Why it exists
In adaptive runs, very slow or noisy tests can struggle to converge before a CI time limit. The budget controller chooses slightly looser precision targets when time is nearly exhausted so tests finish with useful, transparent results rather than timing out or collecting excessive samples.

## How it works (v1)
1. Minimum/pilot complete → estimate median per‑iteration duration (considering `OperationsPerInvoke`).
2. Compute remaining time from `MaxMeasurementTimePerMethodMs`.
3. Approximate how many more iterations are feasible under the remaining budget.
4. If the estimate is “tight” (few iterations remain), relax targets within conservative caps:
   - `TargetCoefficientOfVariation` may be relaxed up to 0.20
   - `MaxConfidenceIntervalWidth` may be relaxed up to 0.50 (relative CI width)
5. Apply adjustments once before the first convergence check; never tighten, never exceed caps.
6. Log an informational message:
   - `Budget controller: remaining={RemainingMs:F1}ms, est/iter={PerIterMs:F2}ms, TargetCV {OldCv:F3}->{NewCv:F3}, MaxCI {OldCi:F3}->{NewCi:F3}`

## How to enable

### Per‑class (attribute)
```csharp
[Sailfish(
    UseTimeBudgetController = true,
    MaxMeasurementTimePerMethodMs = 30_000 // 30 seconds per method
)]
public class MyBudgetedTests { }
```

### Notes
- If `MaxMeasurementTimePerMethodMs` is 0 (default), the controller is inert even if enabled.
- Works alongside Adaptive Sampling. If convergence is met earlier, the time budget is not used.
- `OperationsPerInvoke` and `TargetIterationDurationMs` can be used to shape per‑iteration durations; the controller itself does not change OPI.

## When to use
Use the budget controller when:
- You care about predictable wall‑clock time in CI (e.g., per‑method SLA)
- Some tests are slow/noisy and occasionally miss tight precision targets
- You want graceful, transparent relaxation rather than hard timeouts

Avoid it when:
- Exact precision targets must be preserved regardless of time (compliance/audit)
- You need identical thresholds across runs for longitudinal studies

## Related
- Adaptive Sampling: [/docs/1/adaptive-sampling](/docs/1/adaptive-sampling)
- Attributes reference: [/docs/1/required-attributes](/docs/1/required-attributes)

