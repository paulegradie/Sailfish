# Overhead Neutralization V2 — Implementation Plan

Owner: Augment Agent
Status: Draft v1.0 (to execute now)
Scope: Replace current fixed-delay overhead estimator with a harness-baseline calibration, decouple from adaptive sampling, add drift detection, and lay groundwork for optional OperationsPerInvoke (OPI) auto‑tuning.

## Objectives
- Measure and subtract framework/harness overhead more accurately using the exact invocation path as real tests.
- Make overhead estimation robust and predictable ("iPhone‑polish"), with clear guardrails and diagnostics.
- Keep backward compatibility (existing flags/attributes still work) and avoid breaking public APIs.
- Decouple overhead estimation from adaptive sampling.

## Non‑Goals (for this iteration)
- Full OPI auto‑tuning (design included; gated for later step).
- Cross‑process isolation or CPU pinning; we rely on drift detection and iteration duration targets.

## Design Summary
1. HarnessBaselineCalibrator
   - Calibrate per‑iteration overhead (ticks) by invoking no‑op probe methods through the exact same reflection/timer path as real tests.
   - Choose probe signature to match the test’s method (sync/async; with/without CancellationToken).
   - Use outer Stopwatch to capture total ticks for each probe invocation (includes reflection + timer start/stop cost).
   - Warmup JIT: run a small number of unrecorded probes.
   - Measure multiple samples; compute median (robust to outliers). Clamp to non‑negative int.
   - Optional second calibration at end for drift detection.

2. Integration
   - In TestCaseIterator, replace OverheadEstimator usage with HarnessBaselineCalibrator.
   - Run BeginCalibration → cache per‑method overhead (ticks).
   - Execute warmups/iterations as today.
   - Run EndCalibration → compare with begin; if drift > threshold (e.g., 20%), surface a soft warning.
   - Apply overhead subtraction via existing CoreInvoker/PerformanceTimer pipeline.
   - Keep DisableOverheadEstimation semantics. Remove coupling between overhead and adaptive sampling.

3. Guardrails & Diagnostics
   - Never allow negative elapsed ticks after subtraction.
   - Cap subtraction to a reasonable fraction of observed iteration when extremely small iterations occur (e.g., at most 80% of that iteration’s ticks).
   - Emit concise diagnostics per test case (overhead ticks, sample count, drift %, whether disabled, caps applied).

4. OPI Auto‑Tuning (Optional, gated)
   - Settings: EnableAutoOPI (bool?), TargetIterationDurationMs (double, default ~10–20ms).
   - Pre‑run: probe increasing OPI until median iteration duration >= target.
   - Looping performed within the iteration strategy by repeating ExecutionMethod N times inside a single timed iteration, respecting iteration setup/teardown semantics.
   - Not implemented in this pass; design reserved.

## Public/External Surface
- No new public APIs in this pass. We add internal calibrator and diagnostics.
- Respect existing settings/attributes (DisableOverheadEstimation at run/class/method level).

## Files to Add
- source/Sailfish/Execution/CalibrationProbes.cs
- source/Sailfish/Execution/HarnessBaselineCalibrator.cs

## Files to Modify (minimally)
- source/Sailfish/Execution/TestCaseIterator.cs (swap estimator, decouple from adaptive sampling, add drift warn)
- source/Sailfish/Execution/OverheadEstimator.cs (deprecated path; keep but unused — or re‑route to new calibrator; no breaking change)

## Step‑by‑Step Execution Plan
1) Add CalibrationProbes (sync/async; with/without CancellationToken) — internal static no‑ops.
2) Add HarnessBaselineCalibrator
   - API: Task<int> CalibrateTicksAsync(MethodInfo methodUnderTest, CancellationToken ct)
   - Parameters: sample counts (consts), warmup count; choose probe based on methodUnderTest traits.
   - Implementation: outer Stopwatch around method.TryInvoke(null, ct, new PerformanceTimer()).
   - Return: median ticks (int, non‑negative).
3) Integrate into TestCaseIterator
   - Instantiate calibrator when overhead is enabled.
   - Begin calibration → AssignOverheadEstimate(int ticks) via CoreInvoker.
   - Run test case as normal.
   - End calibration → compare vs begin; compute drift %; attach warning if above threshold.
   - Ensure adaptive sampling stays governed only by ExecutionSettings; do not disable it when overhead is disabled.
4) Apply guardrails
   - IterationPerformance.ApplyOverheadEstimate already avoids negative; additionally cap subtraction per iteration to <= 80% of that iteration’s ticks. (If necessary, add a small cap in PerformanceTimer before subtraction.)
5) Diagnostics
   - Where: attach to TestCaseExecutionResult/TestAdapter user messages (non‑console) in a concise one‑liner and a verbose debug string.
   - Example: "Overhead 342 ticks (median of 64 samples). Drift +3%. Capped 0 iterations."
6) Tests
   - Unit: calibrator returns >0 on typical systems; respects sync vs async vs token paths.
   - Unit: ApplyOverheadEstimate never produces negative; capping logic triggers for extremely small iterations.
   - Integration: iterator applies overhead when enabled; disabled path leaves timings unchanged; adaptive sampling unaffected.
7) Docs
   - Update README / framework docs: brief section on overhead calibration and settings.

## Acceptance Criteria
- All tests pass locally and in CI.
- Overhead subtraction no longer exhibits negative deltas; microbench timings become more stable (lower CV) compared to baseline.
- Diagnostics clearly report overhead and drift; disabling overhead estimation behaves as expected.
- No breaking API changes.

## Risks & Mitigations
- Extremely fast probes may measure ~0 ticks on low‑resolution envs → increase sample count; median still robust.
- Drift due to system load → drift detection warns; consider rerun guidance.
- OPI loops can break stateful tests → gated/opt‑in; not implemented in this pass.

## Rollback Plan
- Keep OverheadEstimator.cs; feature flag via DisableOverheadEstimation remains.

## Work Breakdown (small, verifiable steps)
- [ ] Step 1: Add CalibrationProbes.cs
- [ ] Step 2: Add HarnessBaselineCalibrator.cs (unit tested)
- [ ] Step 3: Integrate into TestCaseIterator; decouple adaptive sampling
- [ ] Step 4: Add drift detection + diagnostics
- [ ] Step 5: Add capping guardrail (<=80% of iteration)
- [ ] Step 6: Tests (unit + integration)
- [ ] Step 7: Docs updates

