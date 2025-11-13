# NextAgentPrompt-2.6 — Precision/Time Budget Controller (Tier A)

Implement an opt‑in controller that relaxes precision targets under tight time budgets to improve UX while preserving statistical rigor.

## Required reading (absolute paths)
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/PrecisionTimeBudgetController-Design-v1.md
- G:/code/Sailfish/source/Sailfish (search for): AdaptiveIterationStrategy, AdaptiveParameterSelector, ExecutionSettings, SailfishAttribute → settings mapping
- G:/code/Sailfish/source/Tests.Library (search for): Adaptive sampling tests and iteration strategy tests

## Scope
- Add a boolean toggle to enable the controller (default: false) at both attribute and settings levels
- When enabled AND a per‑method time budget is set, after the pilot/minimum phase compute remaining budget and per‑iteration cost and (if tight) relax thresholds within conservative caps
- Emit a clear informational log when adjustments are applied
- Backward compatible: No changes when disabled or when no budget is set

## Design sketch
- Properties (opt‑in):
  - ExecutionSettings: `UseTimeBudgetController` (bool)
  - SailfishAttribute: `UseTimeBudgetController` (bool) → mapped into ExecutionSettings
- Integration point: inside AdaptiveIterationStrategy just before first convergence check, after AdaptiveParameterSelector tuning
- Heuristic (v1): as documented in the design file — median pilot per‑iter estimate; allowed iterations threshold; factors 1.25–2.0; caps CV≤0.20 and CI≤0.50; never tighten vs current
- Logging format (Information):
  - "Budget controller: remaining={RemainingMs:F1}ms, est/iter={PerIterMs:F2}ms, TargetCV {OldCv:F3}->{NewCv:F3}, MaxCI {OldCi:F3}->{NewCi:F3}"

## Proposed file touchpoints (exact class names may vary; locate via search)
- source/Sailfish/Execution/IterationStrategies/AdaptiveIterationStrategy*.cs (inject controller step)
- source/Sailfish/Execution/Adaptive/AdaptiveParameterSelector*.cs (read current thresholds)
- source/Sailfish/Attributes/SailfishAttribute.cs (add property; keep default false)
- source/Sailfish/Extensions/ExecutionExtensionMethods*.cs (map attribute → settings)
- source/Sailfish/Execution/ExecutionSettings.cs (add `UseTimeBudgetController`)
- Tests: source/Tests.Library/*Adaptive*/ and new unit tests for controller behavior

## Steps
1) Add settings and attribute properties; wire mapping (default false)
2) Implement small utility/controller (pure function) that computes relaxed thresholds given (remainingMs, perIterMs, currentTargets)
3) Call controller from AdaptiveIterationStrategy v1 hook; apply only when enabled and budget set
4) Add unit tests for controller (tight vs generous budget); add one integration test to assert adjusted thresholds flow into convergence check (optional v1)

## Acceptance criteria
- Disabled by default; no behavioral change when off or without budget
- When enabled and budget tight, thresholds are relaxed within caps and info log emitted
- All existing tests pass on net8.0/net9.0; new tests green

## Verification commands (safe, targeted)
- dotnet build G:/code/Sailfish/source/Sailfish.sln -v:m
- dotnet test G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj -c Debug --filter TestCategory!=Slow -m:1 -v:m

## Notes
- Keep changes minimal and localized; prefer small, well‑named components
- Preserve backward compatibility; avoid public API breaks beyond new opt‑in properties
- If uncertain about integration point names, search by responsibility and confirm with small targeted reads before editing

