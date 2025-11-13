# NextAgentPrompt-2.8 — Adaptive Parameter Selector (Tier A completion)

Implement the remaining, scoped parts of the Adaptive Parameter Selector to deliver an "iPhone‑level" experience while preserving backward compatibility. This step refines the pilot‑based tuning used by Adaptive Sampling so thresholds feel smart out‑of‑the‑box, without breaking existing behavior or user overrides.

## Required reading (absolute paths)
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/Sailfish_iPhone_Level_Polish_PRD.md (sections around P: Adaptive Parameter Selector)
- G:/code/Sailfish/source/Sailfish/Execution/AdaptiveIterationStrategy.cs
- G:/code/Sailfish/source/Sailfish/Execution/AdaptiveSamplingConfig.cs
- G:/code/Sailfish/source/Sailfish/Analysis/AdaptiveParameterSelector.cs
- G:/code/Sailfish/source/Tests.Library/Analysis/AdaptiveParameterSelectorTests.cs
- G:/code/Sailfish/site/src/pages/docs/1/adaptive-sampling.md
- G:/code/Sailfish/source/Sailfish/Execution/ExecutionSettings.cs (MinimumSampleSize, MaximumSampleSize, TargetCoefficientOfVariation, MaxConfidenceIntervalWidth)
- G:/code/Sailfish/source/Sailfish/Attributes/SailfishAttribute.cs (mapping surface)
- G:/code/Sailfish/source/Sailfish/Execution/AdaptiveIterationStrategy.cs (integration point already using selector)

## Objective
- Expand the selector’s recommendations beyond just CV/CI to include a recommended MinimumSampleSize (and optionally an OutlierStrategy hint), while respecting user inputs.
- Apply recommendations locally within AdaptiveIterationStrategy (not global state), with conservative, non‑breaking rules.
- Update docs to explain how the selector classifies speed and chooses sensible defaults from pilot samples.

## Non‑Goals (v2.8)
- Do not change public APIs in a breaking way.
- Do not override user‑provided thresholds with stricter values.
- Do not change MaximumSampleSize automatically; keep the user’s explicit cap.
- Do not couple selector directly to OperationsPerInvoke tuning (already implemented separately).

## Scope
- Enhance AdaptiveSamplingConfig to carry a recommended MinimumSampleSize and an optional rationale string (SelectionReason). Keep existing properties intact.
- Update AdaptiveParameterSelector to compute a MinimumSampleSize suggestion based on speed category.
- Integrate the suggested MinimumSampleSize inside AdaptiveIterationStrategy as a local floor for the first convergence check: `effectiveMin = Math.Max(executionSettings.MinimumSampleSize, config.RecommendedMinimumSampleSize)`.
- Preserve the current rules already implemented for CV and CI suggestions:
  - Never tighten beyond the user’s requested CV (i.e., do not recommend a lower, stricter CV than the user asked).
  - Never exceed the user’s MaxConfidenceIntervalWidth (i.e., do not recommend a looser CI budget than the user allowed).
- Log a concise info line with category and applied budgets.

## Design sketch
- File: G:/code/Sailfish/source/Sailfish/Execution/AdaptiveSamplingConfig.cs
  - Add:
    - `public int RecommendedMinimumSampleSize { get; }`
    - `public string SelectionReason { get; }`
  - Add an additional constructor that includes these fields while keeping the existing ctor for back‑compat.

- File: G:/code/Sailfish/source/Sailfish/Analysis/AdaptiveParameterSelector.cs
  - Extend `Select(...)` to compute `RecommendedMinimumSampleSize` (by category) and a short `SelectionReason` (e.g., "UltraFast: raised min N to stabilize CV for microbenchmarks").
  - Proposed category mapping (tune if tests reveal better thresholds):
    - UltraFast (< 50µs median): RecommendedMinimumSampleSize = 40–60 (start at 50)
    - Fast (< 0.5ms): 30
    - Medium (< 5ms): 20
    - Slow (< 50ms): 15
    - VerySlow (≥ 50ms): 10

- File: G:/code/Sailfish/source/Sailfish/Execution/AdaptiveIterationStrategy.cs
  - Where the selector is already used, consume `RecommendedMinimumSampleSize` to set the effective minimum for the first (and subsequent) convergence checks:
    - `var minSamples = Math.Max(executionSettings.MinimumSampleSize, selected.RecommendedMinimumSampleSize);`
    - Use `minSamples` instead of the raw `executionSettings.MinimumSampleSize` in the convergence gate.
  - Keep current behavior for CV/CI assignments and budget controller ordering.
  - Log: `---- Adaptive tuning: {Category} -> MinN={Min}, TargetCV={Cv:F3}, MaxCI={Ci:F3}`. Include `SelectionReason` if helpful.

## Steps
1) Model update
   - Extend AdaptiveSamplingConfig with `RecommendedMinimumSampleSize` and `SelectionReason`.
   - Keep the current ctor and add a new overload; do not break existing call sites.

2) Selector update
   - Update `AdaptiveParameterSelector.Select(...)` to compute the new fields per category.
   - Ensure the current safety guards remain:
     - `cv = Math.Max(recommendedCv, executionSettings.TargetCoefficientOfVariation)`
     - `ci = Math.Min(recommendedCi, executionSettings.MaxConfidenceIntervalWidth)`

3) Strategy integration
   - In AdaptiveIterationStrategy, replace the hard minimum used for the first convergence check with `effectiveMin` per the design sketch.
   - Add/adjust log line to show category, MinN, CV, CI.

4) Tests
   - Unit: extend `source/Tests.Library/Analysis/AdaptiveParameterSelectorTests.cs` to assert category→`RecommendedMinimumSampleSize` mapping and `SelectionReason` presence.
   - Unit/Integration: add a focused test in `source/Tests.Library/Execution` proving that for UltraFast pilot samples, the strategy respects the higher `effectiveMin` (i.e., does not attempt convergence before that sample count).
   - Backward‑compat: ensure no test relying on prior defaults regresses; where necessary, update assertions to use the new log line while preserving semantics.

5) Documentation
   - Update `site/src/pages/docs/1/adaptive-sampling.md` to add a short "Parameter selection" subsection:
     - How speed categories are determined (median based)
     - How CV/CI and MinN recommendations are chosen
     - The safety rules (never tighter CV than requested; never looser CI than allowed; MinN only increases the convergence gate)
   - Optional: a small tip box explaining why ultra‑fast microbenchmarks benefit from a higher minimum sample size.

6) Release notes & PRD
   - Add a brief note to RELEASE_NOTES.md under the current Unreleased section: "Adaptive parameter selector now also recommends minimal sample count by speed class; integrated conservatively in adaptive sampling (backward compatible)."
   - Mark the item complete in `Sailfish_iPhone_Level_Polish_PRD.md` under the Adaptive Parameter Selector section.

## Acceptance criteria
- Builds green on net8.0/net9.0.
- Adaptive selector returns `RecommendedMinimumSampleSize` and `SelectionReason`.
- AdaptiveIterationStrategy uses `effectiveMin = Max(userMin, recommendedMin)` for convergence checks.
- Existing behavior preserved unless selector provides a higher minimum; users can still enforce their own higher minimum via attribute/globals.
- Tests cover category mapping and strategy honoring of `effectiveMin`.
- Docs updated to explain the pilot‑based parameter selection.

## Verification commands (safe, targeted)
- cd /d G:/code/Sailfish && cd   // confirm CWD
- dotnet build source/Sailfish.sln -c Debug -v:m
- dotnet test "G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj" -c Debug --filter TestCategory!=Slow -m:1

## Notes
- Keep the selector simple and robust: rely on medians and clear category thresholds.
- Avoid thrashing: recommendations are computed once after the minimum/pilot phase.
- Do not add per‑iteration allocations; keep the hot path tight.
- If category thresholds need tuning, prefer conservative defaults and adjust tests accordingly.

