# Project: Opt-in Process Isolation ("deep isolation mode")

> Let a benchmark (or a whole run) opt in to executing in a **dedicated child process** with a
> controlled launch environment, so benchmarks that need genuinely robust results are no longer
> contaminated by the shared test host.

**Status:** `Planning` · **Phase:** P0 (spec) · **Owner:** Paul · **Created:** 2026-06-03

---

## TL;DR

Sailfish runs **100% in-process** today. When run as a test (IDE / `dotnet test`), the dominant
source of measurement noise is the **test host itself** — other tests in the run, the vstest/IDE
infrastructure, its background threads, GC pressure, and JIT activity, all sharing your process.

The existing `[Sailfish(Lifetime = PerCase)]` gives **object** isolation (fresh instance + DI scope
per case). It does **not** touch any process-level noise, and it cannot — server/workstation GC and
JIT tiering are fixed at process launch. "Deep isolation that produces robust results" therefore
*requires* getting the benchmark out of the shared host into a dedicated child process. That is the
BenchmarkDotNet toolchain model, and this project adds it to Sailfish as an **opt-in**.

The good news, established during planning:

1. **No codegen/build step needed.** Sailfish's engine is reflection/DI-driven over discovered
   types, so the child host just loads the already-built test assembly and runs the existing engine
   on a selected class. (BDN has to generate + build a project precisely because it has no such
   engine.)
2. **The seam already exists.** [`ClassExecutionDispatcher`](../../../source/Sailfish/Execution/ClassExecutionDispatcher.cs)
   is the single shared fork point for the class's `Lifetime`; isolation forks at the same place,
   and the *same dispatcher* runs on both sides of the process boundary.
3. **The IPC payload already exists.** The `Tracking.V1` serialization
   ([`PerformanceRunResultTrackingFormat`](../../../source/Sailfish/Contracts.Public/Serialization/Tracking.V1/PerformanceRunResultTrackingFormat.cs))
   already round-trips raw per-iteration samples (`double[] RawExecutionResults`) to disk for
   SailDiff. The child emits a tracking blob; the parent ingests it exactly as it already ingests a
   completed run. No new data contract required.

## Document map

| Doc | Purpose |
|---|---|
| [`SPEC.md`](./SPEC.md) | **The context + design.** Current-architecture facts, the out-of-process model, the seam, the IPC boundary, the config surface, semantics that change, risks, alternatives considered. Read this first. |
| [`PLAN.md`](./PLAN.md) | **The steps.** Phased, stacked-PR implementation plan (P0–P5), each step with concrete file targets and acceptance criteria. The executable roadmap to completion. |
| [`HANDOFF.md`](./HANDOFF.md) | **Living state.** Current phase/step, what's done, the single next action, decision log, open questions, and gotchas discovered along the way. **Update this every work session.** |

## Status at a glance

| Phase | Title | State |
|---|---|---|
| P0 | Spec & scaffolding (this PR) | 🟡 In review |
| P1 | Config surface + effective-isolation resolver | ⬜ Not started |
| P2 | The isolated host (in-process-equivalent) | ⬜ Not started |
| P3 | Parent-side spawn + result round-trip | ⬜ Not started |
| P4 | Launch-environment knobs (GC / JIT / priority / affinity) | ⬜ Not started |
| P5 | Packaging, analyzer, docs, hardening | ⬜ Not started |

## Open decisions (need Paul's call — see HANDOFF "Decision log")

1. **Default process granularity** — per-class (recommended) vs per-case.
2. **Host shipping mechanism** — `dotnet exec` a packaged framework-dependent dll (recommended) vs per-RID native exe.
3. **`Robust` preset** — new `SailfishPreset` value vs an orthogonal switch.
4. **Analyzer ID** — claim next free `SFxxxx` (SF1024 is taken; verify against `AnalyzerReleases.Unshipped.md`).
