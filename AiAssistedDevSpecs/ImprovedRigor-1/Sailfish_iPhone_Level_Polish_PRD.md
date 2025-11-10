# Sailfish “iPhone‑Level Polish” Implementation Spec (ImprovedRigor‑1)

Version: 1.0
Owner: Sailfish Core
Status: Draft for implementation

## 1) Objective
Deliver a production‑grade, predictable, and delightful performance benchmarking experience with statistical rigor comparable to Benchmark.NET while keeping Sailfish’s strengths (test integration, comparisons). This document consolidates scope from existing plans and adds missing polish features. It breaks work into implementable units with code touchpoints and tests.

## 2) In‑scope (Phaseable)
Tier A (Essentials)
- Environment Health Check (+ “Sailfish Doctor” quick command)
- Reproducibility Manifest persisted with each run (Ignore)
- Anti‑DCE guard rails (consumer API + Roslyn analyzer)
- OperationsPerInvoke + TargetIterationDuration auto‑tuning 
- Precision/Time budgets (target CI width and/or max run time)
- Multiple comparisons correction (FDR) + effect size CIs in NxN 
- Seeded randomized run order
- Variance decomposition (if multi‑launch enabled)

Tier B (High‑value)
- Lightweight diagnosers by default (Memory, GC, Threading, JIT/Tiered hints)
- Out‑of‑process rigorous harness option (affinity, priority, isolation)
- Timer calibration and jitter scoring

Tier C (UX/Stats Enhancements)
- Quality Grade/Confidence badges per test
- Rich console/markdown output (sparklines, percentiles, copy re‑run cmd)
- Analyzer pack for hot‑path anti‑patterns
- Bootstrap CIs for ratios/medians (opt‑in)
- Trend/drift detection during iteration
- Power/precision planning from pilot phase

Out of scope:
- Non‑.NET targets; full perf‑ETW UI; long‑term data storage service.

## 3) Dependencies and current groundwork
- Adaptive sampling, CI integration, and console CI display: see Statistical Engine Handoff and Upgrade Plan.
- Existing TestCaseIterator, ExecutionSettings, RunSettings; Outlier detector; SailDiff.

## 4) Architecture overview (new/updated components)
- Diagnostics.EnvironmentHealth: probes environment and returns a scored report.
- Results.ReproducibilityManifest: immutable record attached to each run/class/method result; persisted next to JSON outputs and embedded in markdown summary.
- Execution.OperationsPerInvokeTuner: calibrates ops/invoke to meet target iteration duration.
- Execution.BudgetController: coordinates precision/time budget with adaptive sampling + ops/invoke.
- Analysis.MultipleComparisons: FDR (Benjamini–Hochberg) module applied to NxN matrices.
- Analysis.AdaptiveParameterSelector: selects sensible defaults from pilot samples.
- Scheduling.SeededOrderer: shuffles methods deterministically by seed within groups.
- Diagnostics.TimerCalibrator: measures timer resolution/jitter and suitability score.
- Diagnosers.(Memory|Threading|Jit): lightweight collectors using EventCounters/Runtime APIs.
- CLI/Doctor: “sailfish doctor” command or a test‑adapter surfaced preflight report.
- Analyzers: Sailfish.Analyzers project with rules SF1xxx.

## 5) Detailed design by feature (with code touchpoints and tests)

A. Environment Health Check + “Doctor”
- Goals: Detect common noise sources; provide fix steps; compute HealthScore [0..100].
- Checks (cross‑platform best effort):
  - Build mode (Release), Tiered JIT/OSR status, RyuJIT
  - Power plan/performance mode; CPU frequency scaling signal; thermal throttling hint
  - Background CPU load (sampled); process priority; CPU affinity pinning option
  - Timer source and resolution; GC mode; server GC; NUMA awareness
- Outputs: structured report + summary badge; integrate into markdown and console header.
- Code:
  - New: source/Sailfish/Diagnostics/Environment/EnvironmentHealthChecker.cs
  - New: source/Sailfish/Diagnostics/Environment/EnvironmentHealthReport.cs
  - New: source/Sailfish.CLI/DoctorCommand.cs (or integrate into existing CLI)
  - Touch: source/Sailfish.TestAdapter/... to display summary badge at run start
- Tests:
  - Unit: synthetic mocks for each probe (simulate poor/good conditions)
  - Integration: run “doctor” in CI, assert minimum information available

B. Reproducibility Manifest
- Fields: Sailfish version; commit SHA; .NET runtime; OS; CPU model; GC mode; Tiered JIT/OSR; process priority; CPU affinity; power plan (if available); timer source/frequency; environment health score; random seed; test order; OperationsPerInvoke; Min/Max/Actual N; CI config; overhead estimator version.
- Lifecycle: captured once per run; per test class append method‑level deltas (N, ops/invoke, CI, diagnoser snippets).
- Persistence: embed in run session JSON; write manifest.json next to results; render short block in markdown.
- Code:
  - New: source/Sailfish/Results/ReproducibilityManifest.cs
  - Touch: source/Sailfish/Tracking/* to persist; SailfishConsoleWindowFormatter to render 1‑line summary
- Tests: JSON schema tests; golden markdown excerpt; reproducibility diff stable across runs when env stable.

C. Anti‑DCE guard rails
- Consumer API: Utilities.Consumer.Consume<T>(T value) using Volatile.Write/GC.KeepAlive to prevent elimination; and NonOptimizable attribute for fields.
- Analyzer rules (Sailfish.Analyzers):
  - SF1001: Discarded return in [SailfishMethod] hot path
  - SF1002: Empty hot path detected
  - SF1003: Console/Debug writes in hot path
  - SF1004: DateTime.Now/Random inside hot loop
  - SF1005: LINQ/alloc heavy ops in hot loop (suggest precompute)
- Code:
  - New: source/Sailfish/Utilities/Consumer.cs
  - New: analyzers/Sailfish.Analyzers/*.csproj (respect RS1035: no Console)
- Tests: Analyzer unit tests (Verifier harness); runtime smoke confirming code isn’t optimized away.

D. OperationsPerInvoke + TargetIterationDuration
- Add to ExecutionSettings and attributes:
  - int OperationsPerInvoke {get;set;} (default 1)
  - TimeSpan TargetIterationDuration {get;set;} (default 2–10 ms configurable)
- Tuning algorithm: pilot iterations measure median iteration time; set ops = ceil(target/median) bounded [1, MaxOps]. Re‑evaluate when variance high.
- Apply ops in iteration loop: repeat measured body ops times inside each iteration.
- Code:
  - Touch: source/Sailfish/Execution/ExecutionSettings.cs (+ interface)
  - Touch: source/Sailfish/Attributes/SailfishAttribute.cs
  - New: source/Sailfish/Execution/OperationsPerInvokeTuner.cs
  - Touch: source/Sailfish/Execution/TestCaseIterator.cs and CoreInvoker to support ops loop
- Tests: unit for tuner math; integration for microbench showing improved timer suitability.

E. Precision/Time budgets controller
- Config:
  - double TargetRelativeCIWidth (e.g., 0.20 default)
  - TimeSpan MaxMeasurementTimePerMethod (optional)
- Controller decides when to stop: converge (CV && CI width) OR time budget hit; log reason.
- Code:
  - New: source/Sailfish/Execution/BudgetController.cs (used by AdaptiveIterationStrategy)
  - Touch: source/Sailfish/Execution/AdaptiveIterationStrategy.cs to honor time budget
- Tests: bounded runtime test; exact stop reasons.

F. Multiple comparisons correction + effect size CIs
- Apply Benjamini–Hochberg FDR to p‑values in NxN; annotate significant cells.
- Show effect size as ratio with CI; interpret labels: Improved/Similar/Slower based on CI crossing 1.0 and configured thresholds.
- Code:
  - Touch: source/Sailfish.SailDiff/* comparison matrix builder and renderers
  - New: source/Sailfish.SailDiff/Statistics/MultipleComparisons.cs
- Tests: synthetic datasets verifying FDR behavior and labels; markdown/CSV golden files.

G. Seeded randomized run order
- Add RunSettings.Seed (nullable). If set, shuffle method cases within comparison groups.
- Log seed; store in manifest; support “replay seed”.
- Code: Touch test discovery/scheduler component and results manifest.
- Tests: deterministic order with fixed seed.

H. Variance decomposition (multi‑launch)
- If multiple isolated process launches enabled, compute components: within‑iteration, within‑process, between‑process; show percentages and recommendations.
- Code: extend session aggregator to compute ANOVA‑like components.
- Tests: synthetic data split verifying decomposition math.

I. Lightweight diagnosers (default‑on, cheap)
- Memory/GC: allocated bytes/op, GC counts via EventCounters; opt‑out via settings.
- Threading: context switch/sample rate estimates; basic contention hints.
- JIT/Tiered: show tier transitions if any (sampled), warn if still Tier0.
- Code: source/Sailfish/Diagnosers/* with pluggable interface; registration in DI; config flags in ExecutionSettings/RunSettings.
- Tests: unit on parsers; integration to ensure negligible overhead by default.

J. Timer calibration and jitter score
- Measure min measurable interval and jitter; produce TimerSuitabilityScore (0..100) vs target durations.
- Recommend increasing OperationsPerInvoke when unsuitable.
- Code: source/Sailfish/Diagnostics/TimerCalibrator.cs; hook into pre‑run and manifest.
- Tests: unit for scoring function.

K. UX output improvements
- Console/Markdown: put N at top; show Mean, Median, 95% CI (±), Min/Max, percentiles; small histogram/sparkline; “copy re‑run command”; links to JSON; adaptive decimals (4→6→8→0 policy already preferred).
- Code: source/Sailfish.TestAdapter/Display/TestOutputWindow/SailfishConsoleWindowFormatter.cs and Markdown/CSV writers.
- Tests: golden outputs and formatting for small/large values.

L. Analyzers pack (additional rules)
- Additional SF1xxx rules: allocations in loops, locking in hot path, async in hot path, logging in hot path.
- Tests: analyzer verifiers.

M. Bootstrap CIs (opt‑in)
- For ratios and medians: implement bootstrap/BCa option for CI computation when data non‑normal.
- Code: source/Sailfish/Analysis/Bootstrapper.cs; toggle in settings.
- Tests: known distributions; stability under resampling.

N. Trend/drift detection
- Detect monotonic trend (e.g., Mann‑Kendall) across iterations; block convergence if trending; suggest mitigations (increase warmup, isolation).
- Code: source/Sailfish/Analysis/TrendDetector.cs; integrate into AdaptiveIterationStrategy.
- Tests: synthetic drifting series.

O. Power/precision planning
- Estimate N from pilot variance to meet target CI width; log plan vs achieved; suggest bump when missed.
- Code: source/Sailfish/Analysis/PowerPlanner.cs; invoked by BudgetController.
- Tests: math unit tests.

P. Adaptive Parameter Selector (file user has open)
- Path: source/Sailfish/Analysis/AdaptiveParameterSelector.cs
- Purpose: choose MinimumSampleSize, TargetCV, MaxCIWidth, OperationsPerInvoke, OutlierStrategy based on pilot samples.
- API (proposed):
  - AdaptiveSamplingConfig Select(double[] pilotSamples, TimeSpan desiredIteration, bool microBenchmarkHint)
  - Fields in AdaptiveSamplingConfig: MinimumSampleSize, TargetCoefficientOfVariation, MaxRelativeCIWidth, OperationsPerInvoke, OutlierStrategy
- Heuristics:
  - If median iteration < 200µs → microbench: MinimumN ≥ 30, TargetCV = max(0.05, cv*0.6), Ops/invoke to reach ≥ 2ms, OutlierStrategy = RemoveUpper
  - If 200µs–10ms → normal: MinimumN 20, TargetCV 0.05, MaxRelCI 0.20
  - If >10ms → slow: MinimumN 10, TargetCV 0.03, MaxRelCI 0.15
  - If cv > 0.2 → relax TargetCV by 50% and raise MaxN cap
- Tests: unit tests covering thresholds and outputs.

## 6) API and configuration changes (aggregated)
- IExecutionSettings additions:
  - bool UseAdaptiveSampling; double TargetCoefficientOfVariation; int MinimumSampleSize; int MaximumSampleSize; double ConfidenceLevel; double MaxRelativeConfidenceIntervalWidth; bool UseRelativeConfidenceInterval;
  - int OperationsPerInvoke; TimeSpan TargetIterationDuration; TimeSpan? MaxMeasurementTimePerMethod; bool EnableDefaultDiagnosers; int? Seed;
- Attributes/SailfishAttribute mirrors selected settings (opt‑in at class level).
- RunSettings: WithGlobalAdaptiveSampling(...), WithBudgets(...), WithDefaultDiagnosers(...), WithSeed(...).

## 7) File touchpoints matrix (non‑exhaustive, starting points)
- source/Sailfish/Execution/ExecutionSettings.cs (+ interface)
- source/Sailfish/Attributes/SailfishAttribute.cs
- source/Sailfish/Execution/TestCaseIterator.cs, AdaptiveIterationStrategy.cs, PerformanceTimer/CoreInvoker
- source/Sailfish/Diagnostics/Environment/*, Diagnostics/TimerCalibrator.cs
- source/Sailfish/Results/ReproducibilityManifest.cs
- source/Sailfish/Execution/OperationsPerInvokeTuner.cs, BudgetController.cs
- source/Sailfish/Analysis/MultipleComparisons.cs, TrendDetector.cs, PowerPlanner.cs, Bootstrapper.cs, AdaptiveParameterSelector.cs
- source/Sailfish.SailDiff/* (matrix building and rendering)
- source/Sailfish.TestAdapter/Display/* (console window, markdown/csv)
- analyzers/Sailfish.Analyzers/*
- source/Sailfish.CLI/DoctorCommand.cs (or equivalent entrypoint)

## 8) Testing strategy
- Unit: math (CI, FDR, bootstrap, planner), tuners, analyzers, diagnostics probes.
- Integration: adaptive sampling with budgets; seeded order determinism; diagnosers enabled; manifest persisted; environment health summary shown.
- Performance: overhead of diagnosers and checks (<10–15% additional; configurable off).
- Cross‑platform sanity: CI matrices on Windows/Linux runners.

## 9) Acceptance criteria (Tier A must‑have)
- Health report printed and embedded; manifest saved; seed logged and replayable.
- Anti‑DCE API present; analyzers catch common mistakes; no Console usage in analyzers.
- OpsPerInvoke tuning keeps microbench iterations within target durations by default.
- Precision/time budgets respected with clear stop reasons.
- NxN comparisons show FDR‑corrected significance and ratio CIs with “improved/similar/slower”.
- Variance breakdown shown when multi‑launch used.
- Tests green; overhead within budget; docs updated.

## 10) Migration and docs
- Update README and ADAPTIVE_SAMPLING_MIGRATION_GUIDE with budgets, ops/invoke, seed, manifest, doctor.
- Add examples demonstrating microbench best practices using Consumer.Consume and OperationsPerInvoke.
- Add troubleshooting: timer unsuitable, environment unhealthy, not converging.

## 11) Rollout plan
- Phase 1 (Tier A core): A, B, C, D, E, F, G (variance optional unless multi‑launch exists)
- Phase 2 (Tier B): diagnosers default‑on; isolation harness; timer calibrator
- Phase 3 (Tier C): UX extras; analyzers pack expansion; bootstrap CIs; drift; power planner

## 12) Risks and mitigations
- Added overhead: keep probes/diagnosers lightweight, feature‑flagged.
- Cross‑platform variability: guard with try/catch and degrade gracefully.
- Statistical misuse: defaults conservative; clear warnings; docs with guidance.

## 13) Next steps (actionable checklist)
- [ ] Confirm API additions to IExecutionSettings and attributes
- [x] Implement EnvironmentHealthChecker + report + console summary (baseline delivered: Build Mode + JIT checks wired to adapter + consolidated markdown; docs + release notes updated)
- [ ] Add ReproducibilityManifest and persist/print
- [ ] Implement Consumer.Consume and minimal analyzers (SF1001–SF1003)
- [x] Implement OperationsPerInvokeTuner; integrate into TestCaseIterator (implemented; unit+integration tests in 2.7; docs updated: /docs/1/required-attributes and /docs/1/iteration-tuning)
- [x] Implement BudgetController and honor MaxMeasurementTimePerMethod (implemented; docs added in 2.6)
- [ ] Apply BH‑FDR in SailDiff; add ratio CI and labels
- [ ] SeededOrderer with logging + manifest
- [x] AdaptiveParameterSelector implementation + tests (file provided)
- [ ] Update docs and add demos; golden output tests

## 14) Handoff for agents (copy‑paste)
- Location: G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/Sailfish_iPhone_Level_Polish_PRD.md
- Read also: Statistical Engine Handoff/Upgrade Plan; Adaptive Sampling Implementation Plan.
- Primary targets to start: ExecutionSettings, EnvironmentHealthChecker, ReproducibilityManifest, OperationsPerInvokeTuner, BudgetController, SailDiff FDR.
- Validate with: dotnet build; focused unit tests for new math; golden output tests for console/markdown.

