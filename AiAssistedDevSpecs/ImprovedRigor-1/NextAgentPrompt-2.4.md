# NextAgentPrompt-2.4 — Timer Calibration + Jitter Scoring (Tier C: Rigor & UX)

This step adds a rigorous timer calibration pass and a “jitter score,” surfaces these in the Reproducibility Manifest and outputs, and wires the baseline overhead (ticks) into our existing overhead neutralization path with guardrails. Keep the implementation fast, dependency‑free, and on by default (opt‑out via runsettings).

## Read this first (context)
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/Sailfish_iPhone_Level_Polish_PRD.md
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/PrecisionTimeBudgetController-Design-v1.md
- G:/code/Sailfish/source/Sailfish/Execution/HarnessBaselineCalibrator.cs
- G:/code/Sailfish/source/Sailfish/Execution/PerformanceTimer.cs
- G:/code/Sailfish/source/Sailfish/Diagnostics/Environment/EnvironmentHealthChecker.cs
- G:/code/Sailfish/source/Sailfish/Results/ReproducibilityManifest.cs

Re-run the repo smoke checks first:
- dotnet build G:/code/Sailfish/source/Sailfish.sln -v:m
- dotnet test G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj -c Debug -f net8.0 -m:1 -v:m

## Goals
1) Implement a timer calibration that:
   - Measures high‑resolution stopwatch frequency and effective resolution (ns)
   - Calibrates baseline call overhead using the same invocation path as harness (consistent with HarnessBaselineCalibrator)
   - Samples enough times to compute dispersion (stddev / MAD / IQR), then derives a JitterScore
2) Persist calibration artifacts:
   - Reproducibility Manifest: resolution, baseline overhead, jitter stats (e.g., RSD%), jitter score
   - Environment Health: add a “Timer Jitter” entry with pass/warn/fail thresholds
   - Console/Markdown: print a small “Timer calibration” block in session header
3) Overhead neutralization
   - Continue to apply baseline overhead using existing ApplyOverheadEstimate() with guardrails (e.g., per‑iteration cap)
   - Track and expose diagnostics (baseline, warmups, samples, capped count)
4) Configurability
   - On by default; allow opt‑out via runsettings flag (e.g., `TimerCalibration=false`)
   - Keep cost minimal (<100ms typical) and stable across platforms

## Design sketch
- Add a small service in Execution (e.g., `TimerCalibrationService`) that returns a result DTO:
  - StopwatchFrequency (long), ResolutionNs (double)
  - BaselineOverheadTicks (int), Warmups (int), Samples (int)
  - Dispersion metrics: StdDevTicks, MedianTicks, RsdPercent (double)
  - JitterScore (0–100, higher is better)
- Sampling method
  - Reuse `HarnessBaselineCalibrator`’s structure/warmups, but expose richer stats (don’t break existing API; you can add an overload/new type)
  - The probe must use the same TryInvoke path as real runs to reflect realistic infrastructure overhead
- JitterScore heuristic
  - Initial mapping suggestion: score = clamp(0, 100, 100 − (RsdPercent * 4)). This yields full score near 0–5% RSD, degrades smoothly, bottoms out around 25% RSD.
  - Keep the mapping centralized and easy to tweak; add XML doc comments explaining rationale
- Persistence and display
  - Repro Manifest: add new nullable fields for the values above (don’t break existing JSON consumers)
  - Environment Health: new entry “Timer Jitter” with Pass/Warn/Fail thresholds (e.g., Pass ≤5%, Warn ≤15%, Fail >15%)
  - Console/Markdown: show a short line block (Resolution, Baseline, JitterScore, RSD%)

## Where to integrate
- Execution startup path (before first test): perform calibration once per session; store results in a provider/singleton accessible to:
  - PerformanceTimer (for diagnostic fields already present)
  - ReproducibilityManifest.CreateBase(...)
  - EnvironmentHealthChecker (augment with a “Timer Jitter” entry)
- Consider `SailfishExecutionEngine` / `SailfishExecutor` as convenient orchestration points; avoid per‑test repetition

## Acceptance criteria
- Build/test pass on net8.0 (and keep net9.0 compatibility)
- Reproducibility Manifest JSON contains the new fields with sensible values
- Console and Markdown include a “Timer calibration” block in session header
- EnvironmentHealthReport includes a “Timer Jitter” entry (Pass/Warn/Fail) and the overall score remains computed sensibly
- Overhead baseline is still used with guardrails; diagnostics (baseline ticks, warmups, samples, capped count) remain visible via PerformanceTimer
- New tests cover:
  - Calibration result ranges and invariants (non‑negative, bounded, sane relationships)
  - Manifest serialization includes new fields
  - Environment health gains the jitter entry
  - Console/Markdown render without crashing (golden text or contains‑checks)

## Proposed file touchpoints
- Add: source/Sailfish/Execution/TimerCalibrationService.cs (or similar)
- Update: source/Sailfish/Execution/HarnessBaselineCalibrator.cs (non‑breaking API extension or adapter)
- Update: source/Sailfish/Results/ReproducibilityManifest.cs (new fields, write logic)
- Update: source/Sailfish/Diagnostics/Environment/EnvironmentHealthChecker.cs (add “Timer Jitter” entry)
- Update: source/Sailfish/Presentation/Console/ConsoleWriter.cs (prepend session header block)
- Update: source/Sailfish/Presentation/Markdown/MarkdownWriter.cs and/or MarkdownTableConverter to include a header block for the session
- Tests: source/Tests.Library/Execution/TimerCalibrationTests.cs (new)
- Tests: source/Tests.Library/Results/ReproducibilityManifestTests.cs (extend)
- Tests: source/Tests.Library/Diagnostics/Environment/... (extend or add)

## Runsettings toggle
- Add support for `TimerCalibration=true|false` in runsettings args parsing. Default true.
- If disabled, skip calibration and mark fields null; do not alter prior behavior.

## Verification commands
- dotnet build G:/code/Sailfish/source/Sailfish.sln -v:m
- dotnet test G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj -c Debug -f net8.0 -m:1 -v:m
- Optional: repeat tests for -f net9.0 if configured locally

## Handoff notes / guardrails
- Keep calibration fast and deterministic; avoid I/O and external processes
- Avoid using Console in analyzers; this work is in the library (Console usage is OK here)
- Do not remove deprecated `OverheadEstimator` yet; we retain it for rollback
- Follow the adaptive decimal formatting guideline: try 4 dp; if 0 then 6; then 8; else 0
- Update RELEASE_NOTES.md to summarize the feature and JSON schema additions

## Done definition
- PR adds the service + integration + tests + docs updates; CI is green; feature demo shows the new header block and manifest fields

