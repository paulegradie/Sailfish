# NextAgentPrompt-2.7 — OperationsPerInvoke + Target Iteration Duration Auto‑Tuning (Tier A)

Implement an opt‑in tuner that sets `OperationsPerInvoke` (OPI) to bring the median per‑iteration time close to a target (e.g., 2–10 ms), improving timer suitability for microbenchmarks and reducing jitter.

## Required reading (absolute paths)
- G:/code/Sailfish/AiAssistedDevSpecs/OverheadNeutralizationV2/IMPLEMENTATION_PLAN.md
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/Sailfish_iPhone_Level_Polish_PRD.md (sections D, J)
- G:/code/Sailfish/source/Sailfish/Attributes/SailfishAttribute.cs (OperationsPerInvoke, TargetIterationDurationMs)
- G:/code/Sailfish/source/Sailfish/Execution/TestCaseIterator.cs, CoreInvoker.cs
- G:/code/Sailfish/source/Sailfish/Execution/PerformanceTimer.cs
- G:/code/Sailfish/source/Tests.Library (search for: microbench, calibration, adaptive parameter selector)

## Scope
- Add a small `OperationsPerInvokeTuner` component (pure logic) that:
  - Runs after warmup/pilot to estimate median iteration time (ns/ms)
  - Computes `ops = clamp(1, MaxOps, ceil(targetDuration / medianIteration))`
  - Returns a tuned `OperationsPerInvoke` to be applied before measured iterations proceed
  - Optionally re-evaluates when dispersion is high
- Integrate into the iteration pipeline such that the measured body is executed `ops` times per iteration
- Expose controls via attribute/settings (already present):
  - `OperationsPerInvoke` (default 1)
  - `TargetIterationDurationMs` (default 0 = disabled)
- Backward compatible: tuner is inert when `TargetIterationDurationMs == 0` and when an explicit OPI is set by the user without a target.

## Design sketch
- New file: `source/Sailfish/Execution/OperationsPerInvokeTuner.cs`
  - Method: `int Tune(double[] pilotSamplesMs, int currentOps, int targetMs)`
  - Heuristics: median‑based; respect caps; stabilize on small changes to avoid thrashing
- Touch points:
  - `TestCaseIterator` (hook the tuner after pilot/minimum, before first convergence check)
  - `CoreInvoker` or inner loop to apply `OperationsPerInvoke`
  - Ensure `ExecutionSettings.OperationsPerInvoke` flows from attribute mapping (already present)
- Logging (Information):
  - `OPI tuner: medianIter={MedianMs:F3}ms, target={TargetMs}ms, ops {OldOps}->{NewOps}`

## Steps
1) Implement `OperationsPerInvokeTuner` with unit tests (slow/fast pilot scenarios)
2) Wire into `TestCaseIterator` (only when `TargetIterationDurationMs > 0`)
3) Ensure the inner measured loop repeats body `ops` times (no extra overhead in measurement accounting)
4) Add integration test asserting per‑iteration median approaches target within tolerance
5) Update docs:
   - `/docs/1/required-attributes` (clarify OPI + target)
   - New page or add to an existing page (short “Iteration Tuning” section)
6) Update release notes and PRD checklist

## Acceptance criteria
- Inert when disabled (TargetIterationDurationMs == 0)
- With target set, OPI is chosen such that median per‑iteration time is within a sensible tolerance band of the target
- No public API breaks; attribute/settings mapping preserved
- Tests green on net8.0/net9.0
- Docs updated

## Verification commands (safe, targeted)
- `cd /d G:/code/Sailfish && cd`  (confirm CWD)
- `dotnet build source/Sailfish.sln -c Debug -v:m`
- `dotnet test "G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj" -c Debug --filter TestCategory!=Slow -m:1`

## Notes
- Prefer a simple first version; avoid complex re‑tuning loops
- Consider interaction with the Budget Controller (already implemented): better OPI improves iteration granularity under budgets
- Avoid per‑iteration allocations in the inner ops loop; keep code‑path hot

