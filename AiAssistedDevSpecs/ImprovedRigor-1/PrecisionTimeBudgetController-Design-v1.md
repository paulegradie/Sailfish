# Precision/Time Budget Controller — Design v1

Date: 2025-11-09
Owner: Augment Agent
Scope: Tier A “iPhone-level polish” — precision/time budget control integrated with Adaptive Sampling

## Objective
Provide a minimal, backward-compatible controller that adapts precision targets (TargetCV and MaxCI width) based on remaining time budget so tests converge within MaxMeasurementTimePerMethod when enabled.

## Non-Goals
- No changes to default behavior unless explicitly enabled
- No changes to fixed-iteration semantics beyond existing early-stop
- No predictive convergence modeling in v1 (heuristic-based only)

## User Experience
- Opt-in: `[Sailfish(UseTimeBudgetController = true, ...)]`
- When enabled and a per-method time budget is set, the controller:
  - After minimum/pilot phase, estimates remaining time and per-iteration cost
  - If budget appears tight, relaxes thresholds slightly to encourage convergence
  - Logs an informational message describing the adjustment
- Never makes thresholds stricter than what the user provided or selector chose

## API Changes
- ExecutionSettings: `bool UseTimeBudgetController { get; set; } = false;`
- SailfishAttribute: `bool UseTimeBudgetController { get; set; } = false;`
- ExecutionExtensionMethods: map attribute -> settings

## Integration Points
- AdaptiveIterationStrategy after pilot analysis and AdaptiveParameterSelector tuning, just before the first convergence check:
  1) Compute remaining budget: `remaining = MaxMeasurementTimePerMethod - (now - testStart)`
  2) Estimate per-iteration time: median of pilot samples (ns -> ms)
  3) Compute allowed iterations: `allowed = floor(remaining / perIterMs)`
  4) If allowed is small (e.g., < 5), relax thresholds:
     - `targetCV' = min(capCV, max(targetCV, targetCV * factor))`
     - `maxCI'   = min(capCI, max(maxCI,   maxCI   * factor))`
  5) Log: "Budget controller: remaining ~X ms; est ~Y ms/iter; relaxing TargetCV A→B, MaxCI C→D"

## Heuristic (v1)
- Per-iteration estimate: `medianNs / 1e6` (ms)
- Tightness: `allowedIterations = floor(remainingMs / perIterMs)`
- Adjustment factor:
  - If `allowed <= 1`: factor = 2.0 (max relax)
  - Else if `allowed <= 3`: factor = 1.5
  - Else if `allowed <= 5`: factor = 1.25
  - Else: factor = 1.0 (no change)
- Caps (safety, align with conventions):
  - TargetCV cap: 0.20
  - MaxCI cap (relative): 0.50
- Monotonic rule: never reduce (tighten) thresholds compared to current values

## Backward Compatibility
- Disabled by default; no behavioral change unless explicitly enabled
- Only relaxes thresholds; never tightens below user-provided/selector values
- Keeps existing logging and flow; adds an extra info log

## Logging
- Level: Information
- Format: `"      ---- Budget controller: remaining={RemainingMs:F1}ms, est/iter={PerIterMs:F2}ms, TargetCV {OldCv:F3}->{NewCv:F3}, MaxCI {OldCi:F3}->{NewCi:F3}"`

## Testing Strategy
1) Unit tests for controller:
   - Tight budget => thresholds relaxed
   - Generous budget => thresholds unchanged
2) Integration via AdaptiveIterationStrategy:
   - With UseTimeBudgetController=true and small remaining budget after pilot, assert convergence detector is called using relaxed thresholds (optional in v1; focus on controller unit tests for determinism)
3) Non-regression: existing IterationStrategy tests continue to pass

## Risks & Mitigations
- Risk: Over-relaxing thresholds may reduce statistical rigor
  - Mitigation: conservative caps; opt-in; clear logging
- Risk: Estimation error due to pilot variability
  - Mitigation: median-based estimate; small factors; follow-up tuning in v2

## Future Enhancements (v2+)
- Budget-aware iteration planning to choose next sample batch size
- Dynamic adjustment during the post-minimum loop
- Incorporate variance and CI evolution models

## Acceptance Criteria
- New property surfaces through attribute -> settings mapping
- Controller adjusts thresholds only when enabled AND a time budget is set
- Informational log emitted on adjustment
- Unit tests pass; no existing tests broken

