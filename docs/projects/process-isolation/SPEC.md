# SPEC — Opt-in Process Isolation

**Status:** Planning (P0) · **Last updated:** 2026-06-03

This document is the design of record. It captures the context, the architecture facts the design
relies on (with file references), the proposed design, the configuration surface, the semantics that
change, the risks, and the alternatives considered. The step-by-step build is in [`PLAN.md`](./PLAN.md);
live progress is in [`HANDOFF.md`](./HANDOFF.md).

---

## 1. Context & problem statement

### 1.1 What we have today

Sailfish's benchmark execution is **entirely in-process**. There is no `Process.Start` anywhere in
the core measurement path; the only process spawning in the repo is in diagnostics
(`ComplexityHistoryStore`, `EnvironmentHealthChecker`). A benchmark runs inside whichever host
invoked it:

- **IDE / `dotnet test`** — inside the vstest `testhost` process, via the test adapter
  ([`TestExecutor`](../../../source/Sailfish.TestAdapter/TestExecutor.cs),
  [`TestDiscoverer`](../../../source/Sailfish.TestAdapter/TestDiscoverer.cs)).
- **Programmatic** — inside the caller's process, via
  [`SailfishRunner.Run`](../../../source/Sailfish/SailfishRunner.cs) →
  [`SailfishExecutionCaller`](../../../source/Sailfish/Execution/SailfishExecutionCaller.cs).

### 1.2 Why that limits robustness

When a benchmark runs as a test, **the test host is the dominant noise source**: other tests in the
same run, the vstest/IDE machinery, its background threads, its allocations and GC pressure, and its
JIT activity all share the benchmark's process. None of that can be controlled from inside the
process after launch.

Crucially, the knobs that most affect measurement robustness are **fixed at process launch** and
cannot be changed at runtime:

- GC flavor — workstation vs **server**, concurrent vs non-concurrent (`DOTNET_gcServer`, `DOTNET_gcConcurrent`).
- JIT tiering — **tiered compilation off** / quick-JIT-for-loops off / PGO
  (`DOTNET_TieredCompilation`, `DOTNET_TC_QuickJitForLoops`, `DOTNET_TieredPGO`), so steady-state code
  is fully optimized and not changing mid-measurement.

### 1.3 Why `PerCase` is *not* this feature

The shipped `[Sailfish(Lifetime = PerCase)]`
([`SailfishLifetime`](../../../source/Sailfish/Attributes/SailfishLifetime.cs)) gives a fresh
instance + DI scope per case. That is **object** isolation. It does not remove the host, other
tests, the shared GC heap, or JIT carryover. It is orthogonal to this project and must not be
conflated with it in docs or UI.

### 1.4 The ask

> "Make it **optional** to go **deep isolation mode** on test runs for benchmarks that really need
> robust results."

Restated precisely: allow a benchmark class (or a whole run) to **opt in to out-of-process
execution** in a dedicated child process whose **launch environment** (GC / JIT / priority /
affinity) we control. Default behavior is unchanged (in-process).

### 1.5 Levels of isolation (shared vocabulary)

| Level | Isolates | Achievable in-process? | This project? |
|---|---|---|---|
| SharedInstance (default) | nothing | — | no |
| `PerCase` (shipped) | object state + DI scope | yes (already) | no — orthogonal |
| In-process hardening | `GC.Collect` between samples, `PriorityClass`, `ProcessorAffinity` | **partially** (priority/affinity yes; GC/JIT no) | optional cheap add-on |
| **Process isolation** | the whole host: other tests, host threads, GC heap, JIT | **no — needs a child process** | ✅ **primary** |
| Process-per-case | + cross-case heap/JIT carryover | child process per case | ✅ P4/P5 option |

---

## 2. Architecture facts the design relies on

All confirmed by reading the current code on `main` (2026-06-03).

### 2.1 The dispatch seam

[`ClassExecutionDispatcher.Dispatch`](../../../source/Sailfish/Execution/ClassExecutionDispatcher.cs)
is — by its own doc comment — the single place "shared by the main-library executor and the
test-adapter engine so the instance-lifecycle policy lives in exactly one place." It reads the
`[Sailfish]` attribute and forks on `Lifetime`. **Isolation forks at the same point.** And elegantly,
**the same dispatcher runs on both sides**: parent-side it decides "spawn a child instead of running
in-process"; child-side it is the in-process worker honoring `Lifetime` as usual.

```
SailfishExecutor → SailFishTestExecutor → ClassExecutionDispatcher.Dispatch(testType, providers, group)
                                                  │
                       ┌──────────────────────────┴───────────────────────────┐
              effective isolation == InProcess                    effective isolation == Process
                       │                                                       │
        _engine.ActivateContainer(provider, …)            OutOfProcessClassRunner.Run(testType, …)
        (existing in-process path)                         → spawn Sailfish.IsolatedHost
                                                            → child runs Dispatch IN-PROCESS
                                                            → child emits Tracking.V1 blob
                                                            → parent rehydrates + publishes
```

### 2.2 The measurement data and where raw samples live

- The engine ([`SailfishExecutionEngine.ActivateContainer`](../../../source/Sailfish/Execution/SailfishExecutionEngine.cs))
  runs per-case hooks → `IterateOverVariableCombos` → `ITestCaseIterator.Iterate` (the warmup +
  sampling loop) and produces a [`TestCaseExecutionResult`](../../../source/Sailfish/Execution/TestCaseExecutionResult.cs).
- A result carries a [`PerformanceTimer`](../../../source/Sailfish/Execution/PerformanceTimer.cs)
  whose `ExecutionIterationPerformances` is the **raw per-iteration sample list** (per-operation
  ticks, plus overhead-calibration diagnostics). This is the ground-truth measurement data.
- [`ClassExecutionSummaryCompiler`](../../../source/Sailfish/Execution/ClassExecutionSummaryCompiler.cs)
  compiles raw results into statistical summaries; all downstream analysis (outliers, SailDiff,
  ScaleFish) runs from there.

### 2.3 The IPC payload already exists (key finding)

The `Tracking.V1` serialization already persists raw samples and round-trips them:

- [`PerformanceRunResultTrackingFormat`](../../../source/Sailfish/Contracts.Public/Serialization/Tracking.V1/PerformanceRunResultTrackingFormat.cs)
  carries **`double[] RawExecutionResults` (milliseconds)** plus mean/median/stddev/variance, the
  outlier-removed array, upper/lower outliers, sample size, warmup count.
- [`ITrackingFileSerialization`](../../../source/Sailfish/Contracts.Public/Serialization/Tracking.V1/TrackingFileSerialization.cs)
  exposes `Serialize(IEnumerable<ClassExecutionSummaryTrackingFormat>)` /
  `Deserialize(string)`.

This is exactly the format Sailfish writes to disk after a run and reads back for SailDiff's
before/after comparison. **The child→parent channel reuses it**: the child produces a tracking blob;
the parent ingests it as if it were a just-completed run. No new data contract, no fidelity loss —
the parent's analysis already runs from these same raw arrays today.

### 2.4 Notifications are published *inside* the engine

`ActivateContainer` publishes MediatR notifications inline — `TestCaseStarted`, `TestCaseCompleted`,
`TestCaseException`, `TestCaseDisabled`, `TestClassCompleted` — and the test adapter records pass/fail
**solely** from those notifications. Those handlers live in the **parent** and have side effects (IDE
results, console, files). Therefore the design must ensure the **parent owns notification
publishing**; the child must not try to reach the parent's mediator. See §3.3.

### 2.5 DI registration is centralized and reusable

Both entry points build the container via the `AddSailfish` registration extension +
[`SailfishTypeRegistrationUtility`](../../../source/Sailfish/Registration/SailfishTypeRegistrationUtility.cs)
(which auto-discovers `IRegisterSailfishServices` in the configured assemblies). The isolated host
reuses this verbatim — it does not reinvent container construction.

---

## 3. Proposed design

### 3.1 Component: `Sailfish.IsolatedHost` (new console project)

A small console executable (multi-targeted to the supported TFMs) that runs exactly one class's
benchmark in a clean process. Responsibilities:

1. **Parse the job** (args or a job-file path): test assembly path, fully-qualified test type,
   optional method/case selector, the effective `IRunSettings` (serialized), output blob path,
   timeout, and a cancellation signal.
2. **Load the user assembly** + dependencies via `AssemblyLoadContext` + `AssemblyDependencyResolver`
   (deps.json resolution).
3. **Build the container** via `AddSailfish` + `SailfishTypeRegistrationUtility` — *identical* to the
   in-process path — but with a **no-op `IMediator`** (notifications fire into the void here; the
   parent re-derives them; see §3.3).
4. **Run** `ClassExecutionDispatcher.Dispatch` for the selected class. Because the same dispatcher
   runs here, the class's `Lifetime` is honored normally inside the child.
5. **Emit** results as a `Tracking.V1` blob (via `ITrackingFileSerialization.Serialize`) to the
   output path, plus a tiny status sidecar (per-case status / exception text for failures that never
   produced a summary). Exit code communicates catastrophic failure.

> Why a dedicated host and not "re-invoke vstest"? See §6 (Alternatives).

### 3.2 Component: parent-side `OutOfProcessClassRunner` + dispatch branch

Introduce an isolation branch at the dispatch seam (§2.1). When a class's **effective isolation** is
`Process`:

1. Resolve the **granularity**: `PerClass` (one child for the whole class — amortizes process
   startup; natural default) or `PerCase` (one child per case — strictest).
2. Build a `ProcessStartInfo`: locate the host (see §3.5), pass the job, and set the launch
   environment (P4): GC/JIT env vars, `PriorityClass`, `ProcessorAffinity`.
3. Launch, stream logs, await with timeout + cancellation (wired to the adapter's `Cancel()`).
4. Read the child's `Tracking.V1` blob, **deserialize** via `ITrackingFileSerialization`, and
   **rehydrate** into the result/summary shape the in-process path produces.
5. **Publish the notifications** for each case/class from the rehydrated results (started/completed/
   exception), so the adapter's pass/fail and the console/report writers behave identically.

### 3.3 The notification-ownership refactor

`ActivateContainer` currently interleaves measurement with notification publishing and inline
summary compilation (§2.4). To let the parent own publishing cleanly there are two options:

- **P3 minimal (chosen for first cut):** the child uses a **no-op mediator**; the parent re-derives
  and publishes notifications from the rehydrated tracking blob + status sidecar. Notifications are
  effectively "this case started/completed/failed with these samples," and re-deriving them in the
  parent is faithful because the compiler is deterministic over the raw arrays.
- **Cleaner follow-up (optional):** factor the measurement loop (produces raw `TestCaseExecutionResult`)
  apart from the publish/compile step, so both in-process and out-of-process share one publish path.
  Tracked as a P3 stretch / P5 cleanup — not required for correctness.

> ⚠️ Failure fidelity is a first-class requirement, not a nice-to-have. A child crash, non-zero exit,
> or timeout **must** map to a `TestCaseExceptionNotification` for every affected case, or the adapter
> silently drops them. This is explicitly covered by P3 acceptance criteria.

### 3.4 Configuration surface

Mirrors how `Lifetime` and the adaptive-sampling/outlier knobs were added (attribute layer +
run-settings layer + `.sailfish.json` + preset).

**Attribute layer** — [`SailfishAttribute`](../../../source/Sailfish/Attributes/SailfishAttribute.cs):

```csharp
// on/off switch, mirrors SailfishLifetime
public SailfishIsolation Isolation { get; set; } = SailfishIsolation.InProcess;
```

```csharp
public enum SailfishIsolation { InProcess = 0, Process = 1 }
```

A companion **class-level `[ProcessIsolation(...)]`** attribute holds the launch knobs, keeping the
already-large `[Sailfish]` attribute uncluttered and giving the analyzer something precise to bind to:

```csharp
[ProcessIsolation(
    Granularity   = IsolationGranularity.PerClass,   // | PerCase
    Gc            = GcPolicy.Server,                  // Inherit | Workstation | Server
    Concurrent    = true,
    Tiering       = TieringPolicy.Disabled,           // Inherit | Disabled
    Priority      = ProcessPriority.High,             // Inherit | AboveNormal | High | RealTime
    AffinityCore  = -1)]                              // -1 = unpinned
```

**Run-settings layer** — a `ProcessIsolationSettings` object on
[`IRunSettings`](../../../source/Sailfish/Contracts.Public/Models/IRunSettings.cs) /
[`RunSettings`](../../../source/Sailfish/RunSettings.cs), with
`RunSettingsBuilder.WithProcessIsolation(...)`
([`RunSettingsBuilder`](../../../source/Sailfish/RunSettingsBuilder.cs)). This sets the run-wide
default and a global override.

**`.sailfish.json`** — a `ProcessIsolation` section in
[`SettingsConfiguration`](../../../source/Sailfish.TestAdapter/TestSettingsParser/SettingsConfiguration.cs),
mapped through
[`AdapterRunSettingsLoader`](../../../source/Sailfish.TestAdapter/Execution/AdapterRunSettingsLoader.cs).

**Preset** — a `Robust` value on [`SailfishPreset`](../../../source/Sailfish/SailfishPreset.cs) (or an
orthogonal switch — open decision) that enables process isolation + tighter stats in one call.

**Effective-isolation resolver** — precedence: **method/attribute > run-settings override >
run-settings default > built-in default (InProcess)**. Implemented once and unit-tested (P1), used by
the dispatch branch (P3).

### 3.5 Host discovery & packaging

The host must be locatable when the adapter runs from the NuGet cache / build output. Recommended:
ship the host **framework-dependent dll** in the Sailfish package and invoke
`dotnet exec Sailfish.IsolatedHost.dll <job>` targeting the test assembly's TFM (portable; no per-RID
native exe matrix). Resolving the host path from the running adapter and selecting the right TFM is
the fiddliest operational piece — owned by P5.

---

## 4. Semantics that change under process isolation

These are behavioral cliffs that **must be documented** (P5) and ideally analyzer-guarded:

- **`ISailfishFixture<T>` and singletons are process-scoped.** They can be shared across cases/classes
  *within* a process but **cannot** be shared across processes. Under `Process` isolation (especially
  `PerCase`), a fixture is constructed per child. Genuinely-once cross-class sharing is impossible
  out-of-process — call this out loudly.
- **Static / `[ThreadStatic]` state** does not carry across the boundary (often the point, but
  surprising if relied upon).
- **Working directory, environment, and ambient config** are the child's. Tracking-file output paths
  must be coordinated so the parent and child agree on locations.
- **The environment health check / Skipper environment snapshot** now describe the *child*. That is
  the correct, relevant environment for the measurement — but the reported environment changes.
- **Lifetime × granularity composition:** `PerClass` + `SharedInstance` = ctor + GlobalSetup once in
  one child (natural pairing). `PerCase` granularity makes per-class lifetime moot (one case per
  child).

---

## 5. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Silent loss of failures across the boundary | P3 maps crash/non-zero-exit/timeout → `TestCaseExceptionNotification` per case; explicit tests. |
| Host can't be located at runtime (packaging) | P5 owns discovery; `dotnet exec` packaged dll; integration test that installs from the produced package. |
| Per-process startup cost dominates short benchmarks | `PerClass` default amortizes; document the cost; isolation is opt-in for exactly the cases that justify it. |
| Launch knobs don't actually take effect | P4 integration test probes runtime state in the child (`GCSettings.IsServerGC`, etc.) and asserts. |
| Tracking format is a declared **breaking-change contract** | Reuse as-is; do not modify `Tracking.V1` shapes. If a new field is unavoidable, version to `Tracking.V2` — out of scope here. |
| Cross-platform affinity/priority differences | Treat priority/affinity as best-effort; `Inherit`/unpinned defaults; document platform caveats. |

---

## 6. Alternatives considered

- **Re-invoke `dotnet vstest` in single-test mode per case.** Avoids a new exe but is heavier (full
  vstest spin-up per unit of work), fragile (nested test runs, runsettings plumbing, recursion
  guards), and still needs result plumbing back. Rejected.
- **Self-relaunch the entry assembly** (`Environment.ProcessPath` + a sentinel arg). Works for the
  programmatic console path but not for the adapter (the "entry assembly" is the test host), and is
  fragile to how the process was started. Rejected as the general mechanism; the dedicated host
  serves all entry points uniformly.
- **Named pipes / shared memory IPC instead of a tracking blob.** More moving parts for no benefit at
  this granularity (we exchange one payload per class/case). A temp-file `Tracking.V1` blob is simple,
  debuggable, and reuses existing serialization. Revisit only if streaming live per-iteration progress
  to the IDE becomes a requirement.
- **In-process hardening only** (`GC.Collect` between samples, priority, affinity). A cheap partial
  win that does **not** remove the host/other-tests/GC-heap/JIT noise, so it cannot deliver "robust."
  May ship as an independent small add-on but is not a substitute.

---

## 7. Out of scope

- Distributed / multi-node execution (that's Trawl's domain).
- Changing default behavior (isolation is strictly opt-in).
- Modifying the `Tracking.V1` contract.
- A standalone `dotnet sailfish` CLI tool (tracked separately; not required here).
