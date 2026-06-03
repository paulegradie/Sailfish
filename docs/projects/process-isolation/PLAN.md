# PLAN — Opt-in Process Isolation

**Status:** Planning (P0) · **Last updated:** 2026-06-03

The executable roadmap. Stacked PRs, each independently green, each leaving `main` shippable
(isolation stays opt-in and inert until P3 lights it up). Tick boxes as steps land; record actual PR
numbers and discoveries in [`HANDOFF.md`](./HANDOFF.md). Design rationale for every step is in
[`SPEC.md`](./SPEC.md).

**Conventions**
- Each phase = one PR (split if it grows). Branch off the previous phase's branch (stacked).
- Definition of done per phase: code + tests + green CI + the phase's acceptance criteria met +
  `HANDOFF.md` updated.
- Keep `main` behavior unchanged until **P3**. Through P1–P2, `Process` resolves but the dispatcher
  still runs in-process behind the resolver (no observable change).

---

## P0 — Spec & scaffolding  ·  _this PR_

- [x] Write `README.md`, `SPEC.md`, `PLAN.md`, `HANDOFF.md` under `docs/projects/process-isolation/`.
- [ ] Open the PR with the plan (this one).
- [ ] File a tracking issue ("Opt-in process isolation") linking this directory; create sub-issues per phase.
- [ ] Paul resolves the four open decisions (README → "Open decisions"); record outcomes in `HANDOFF.md`.

**Acceptance:** docs merged; tracking issue + phase sub-issues exist; open decisions logged.

---

## P1 — Config surface + effective-isolation resolver  ·  _no behavior change_

Goal: every way of expressing "isolate this" exists and round-trips, and a single resolver computes
the effective isolation for a class. Nothing spawns yet.

- [ ] Add `SailfishIsolation { InProcess = 0, Process = 1 }` enum (`source/Sailfish/Attributes/SailfishIsolation.cs`).
- [ ] Add `Isolation` property to [`SailfishAttribute`](../../../source/Sailfish/Attributes/SailfishAttribute.cs) (default `InProcess`).
- [ ] Add `[ProcessIsolation]` class attribute + its enums (`IsolationGranularity`, `GcPolicy`, `TieringPolicy`, `ProcessPriority`) under `source/Sailfish/Attributes/`.
- [ ] Add `ProcessIsolationSettings` model + `ProcessIsolation` knob to [`IRunSettings`](../../../source/Sailfish/Contracts.Public/Models/IRunSettings.cs) and [`RunSettings`](../../../source/Sailfish/RunSettings.cs).
- [ ] Add `WithProcessIsolation(...)` to [`RunSettingsBuilder`](../../../source/Sailfish/RunSettingsBuilder.cs).
- [ ] Add `ProcessIsolation` section to [`SettingsConfiguration`](../../../source/Sailfish.TestAdapter/TestSettingsParser/SettingsConfiguration.cs) and map it in [`AdapterRunSettingsLoader`](../../../source/Sailfish.TestAdapter/Execution/AdapterRunSettingsLoader.cs).
- [ ] Add the `Robust` preset (or orthogonal switch — per decision #3) to [`SailfishPreset`](../../../source/Sailfish/SailfishPreset.cs).
- [ ] Implement `EffectiveIsolationResolver` (precedence: attribute > run-settings override > run-settings default > InProcess). Pure, no I/O.

**Acceptance:** unit tests cover the resolver precedence matrix and `.sailfish.json` → `IRunSettings`
round-trip; full suite green; **no behavioral change** to any existing run.

---

## P2 — The isolated host (in-process-equivalent)  ·  _not yet wired in_

Goal: a standalone process can run one class and emit a faithful `Tracking.V1` blob, driven directly
(e.g. from a test), independent of the adapter.

- [ ] New project `source/Sailfish.IsolatedHost/` (console exe, multi-targeted to supported TFMs); add to `Sailfish.sln`.
- [ ] Job contract: define the serialized job (assembly path, type, optional case selector, `IRunSettings`, output path, timeout). Put shared types where both host and library see them.
- [ ] Assembly loading via `AssemblyLoadContext` + `AssemblyDependencyResolver` (deps.json).
- [ ] Container build reusing `AddSailfish` + [`SailfishTypeRegistrationUtility`](../../../source/Sailfish/Registration/SailfishTypeRegistrationUtility.cs), with a **no-op `IMediator`** registered.
- [ ] Run [`ClassExecutionDispatcher.Dispatch`](../../../source/Sailfish/Execution/ClassExecutionDispatcher.cs) for the selected class; honor `Lifetime` as usual inside the child.
- [ ] Emit results as `Tracking.V1` via [`ITrackingFileSerialization`](../../../source/Sailfish/Contracts.Public/Serialization/Tracking.V1/TrackingFileSerialization.cs) + a status sidecar for summary-less failures; exit code for catastrophic failure.

**Acceptance:** golden test — running a deterministic benchmark class through the host produces a
tracking blob whose `RawExecutionResults`/summaries are equivalent (within tolerance) to the same
class run in-process; host handles a throwing benchmark without hanging (sidecar + non-zero exit).

---

## P3 — Parent-side spawn + result round-trip  ·  _lights it up_

Goal: when effective isolation is `Process`, the class actually runs out-of-process and the result is
indistinguishable downstream (same pass/fail, equivalent stats), in **both** the programmatic and
adapter paths.

- [ ] `OutOfProcessClassRunner`: build `ProcessStartInfo`, locate the host (stub path for P3; real discovery in P5), launch, stream logs, await with **timeout + cancellation** (wire the adapter's `Cancel()`).
- [ ] Branch in [`ClassExecutionDispatcher`](../../../source/Sailfish/Execution/ClassExecutionDispatcher.cs) (or a decorator): effective isolation `Process` → `OutOfProcessClassRunner`; else existing in-process path.
- [ ] Rehydrate the child's `Tracking.V1` blob into the result/summary shape; **publish notifications in the parent** (started/completed/exception) from the rehydrated results (SPEC §3.3).
- [ ] **Failure fidelity:** child crash / non-zero exit / timeout → `TestCaseExceptionNotification` for every affected case in the group (SPEC §2.4, §3.3).
- [ ] Honor `Granularity`: `PerClass` (one child/class) and `PerCase` (one child/case).
- [ ] _(Stretch)_ factor measurement apart from publish/compile so both paths share one publish path.

**Acceptance:** E2E in adapter + programmatic paths — an isolated run reports identical pass/fail and
equivalent statistics to in-process for the same workload; a benchmark that throws is reported failed
(not dropped); a hanging benchmark is killed at timeout and reported; cancellation propagates to the
child; full suite green.

---

## P4 — Launch-environment knobs

Goal: the dedicated process is actually launched under the requested GC / JIT / priority / affinity —
the reason process isolation buys robustness.

- [ ] Apply env vars from `[ProcessIsolation]` / settings: `DOTNET_gcServer`, `DOTNET_gcConcurrent`, `DOTNET_TieredCompilation`, `DOTNET_TC_QuickJitForLoops`, `DOTNET_TieredPGO`.
- [ ] Apply `Process.PriorityClass` and (best-effort, platform-aware) `Process.ProcessorAffinity`.
- [ ] Finalize the `Robust` preset values (per decision #3).
- [ ] Child-side diagnostic that reports its actual runtime config (server GC, concurrent, tiering) back in the blob/sidecar for assertion + display.

**Acceptance:** integration test asserts the child runs under the requested GC/JIT (probe via
`GCSettings.IsServerGC` etc. in the child and assert in the parent); knobs documented with platform
caveats; full suite green.

---

## P5 — Packaging, analyzer, docs, hardening

Goal: it works installed-from-package, is guarded against foot-guns, and is documented.

- [ ] Package the host into the Sailfish NuGet; implement runtime **host discovery** from the running adapter; invoke via `dotnet exec` against the test assembly's TFM (per decision #2).
- [ ] Add analyzer `SFxxxx` (claim next free ID; SF1024 is taken — verify against [`AnalyzerReleases.Unshipped.md`](../../../source/Sailfish.Analyzers/AnalyzerReleases.Unshipped.md)): warn on cross-process `ISailfishFixture<T>` sharing under `Process` isolation, and on `[ProcessIsolation]` present without `Isolation = Process`. Use `HasAttributeAmong` for attribute detection (not the buggy `.All()` overload).
- [ ] Docs page under the published site (`site/`), and **add it to the manual sidebar** in `site/src/components/Layout.jsx` (orphaned-page gotcha). Cover: what it is, how it differs from `PerCase`, the cross-process fixture/singleton/static semantics (SPEC §4), cost trade-offs.
- [ ] Note in SailDiff/ScaleFish/Skipper docs that the measured environment is the child under isolation.
- [ ] Install-from-package E2E; analyzer unit tests; finalize `PerCase` granularity.

**Acceptance:** a consumer project installing the produced package can opt a class into `Process`
isolation and get results; analyzer fires on the foot-guns with tests; docs live and linked in the
sidebar; full suite (library + adapter + analyzer) green.

---

## Dependency graph

```
P0 ─▶ P1 ─▶ P2 ─▶ P3 ─▶ P4 ─▶ P5
                   │
                   └─ P3 is the first phase with observable behavior; P1–P2 are inert.
```

## Test strategy (cross-cutting)

- **Unit:** resolver precedence; settings round-trip; job (de)serialization; rehydration mapping.
- **Golden/equivalence:** host vs in-process produce equivalent raw samples/summaries for a
  deterministic workload (the core fidelity guarantee).
- **E2E:** adapter + programmatic isolated runs (pass, fail, hang/timeout, cancel).
- **Integration:** launch knobs actually applied (probe runtime state in the child).
- **Packaging:** install-from-package consumer can opt in and run.
