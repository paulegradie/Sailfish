# HANDOFF — Opt-in Process Isolation

> **Living document.** Update this at the end of every work session. It is the single source of
> truth for "where are we and what's next." Keep it short and current — history goes in the session
> log at the bottom, not inline.

**Last updated:** 2026-06-03 · **By:** Claude (planning) · **Current phase:** P0 (spec)

---

## Current state

- Planning complete. `README.md` / `SPEC.md` / `PLAN.md` written and under review in this PR.
- No code written yet. No behavior change. `main` untouched by anything in this directory.

## Next action (do this next)

➡️ **Get the four open decisions resolved (below), then start P1.** P1 is pure config surface +
resolver with no behavior change — safe to land early. Begin with `SailfishIsolation` +
`SailfishAttribute.Isolation` and the `EffectiveIsolationResolver`, since everything else hangs off
the resolved value.

## Phase status

| Phase | State | PR | Notes |
|---|---|---|---|
| P0 Spec & scaffolding | 🟡 In review | _(this PR)_ | docs only |
| P1 Config surface + resolver | ⬜ Not started | — | inert; no behavior change |
| P2 Isolated host | ⬜ Not started | — | not wired in |
| P3 Parent spawn + round-trip | ⬜ Not started | — | first observable behavior |
| P4 Launch-environment knobs | ⬜ Not started | — | the robustness payoff |
| P5 Packaging / analyzer / docs | ⬜ Not started | — | ship-ready |

---

## Decision log

Record every decision here with date + rationale. ✅ decided · ❓ open.

| # | Decision | Status | Resolution / rationale |
|---|---|---|---|
| 1 | Default process **granularity** | ❓ open | Recommend `PerClass` (amortizes process startup; pairs naturally with `SharedInstance`). `PerCase` available as opt-in. |
| 2 | Host **shipping mechanism** | ❓ open | Recommend `dotnet exec` a packaged framework-dependent dll (portable; avoids per-RID exe matrix). |
| 3 | `Robust` **preset** shape | ❓ open | Recommend a new `SailfishPreset.Robust` that enables isolation + tighter stats, vs an orthogonal switch. |
| 4 | **Analyzer ID** | ❓ open | Claim next free `SFxxxx` (SF1024 taken). Verify against `AnalyzerReleases.Unshipped.md` before use. |
| — | Reuse `Tracking.V1` as the IPC payload | ✅ 2026-06-03 | It already round-trips raw samples (`RawExecutionResults`); no new contract, no fidelity loss. |
| — | Fork isolation at `ClassExecutionDispatcher` | ✅ 2026-06-03 | Already the single shared lifetime fork point; same dispatcher runs on both sides of the boundary. |
| — | Parent owns notification publishing | ✅ 2026-06-03 | Adapter records pass/fail solely from MediatR notifications, whose handlers live in the parent. Child uses a no-op mediator. |
| — | Isolation is strictly opt-in | ✅ 2026-06-03 | Default stays `InProcess`; no existing run changes behavior. |

## Open questions for Paul

1. Decisions #1–#4 above.
2. Should in-process hardening (`GC.Collect` between samples / priority / affinity without a child
   process) ship as a separate small add-on, or be folded in / dropped? (Partial win; not a
   substitute for process isolation — see SPEC §6.)
3. Any objection to a new console project in the solution (`Sailfish.IsolatedHost`) vs an alternate
   host-delivery shape?

## Gotchas discovered (append as you hit them)

- **Tracking format is a declared breaking-change contract** (`Tracking.V1/*` headers). Reuse as-is;
  do not add/rename fields. A new field would force `Tracking.V2` — out of scope.
- **Notifications are published *inside* `ActivateContainer`.** Don't let the child publish to a real
  mediator; the parent must re-derive/publish (SPEC §2.4, §3.3).
- **Docs site sidebar is a hand-coded array** in `site/src/components/Layout.jsx` — a new page is
  invisible until added there (P5).
- **Analyzer attribute detection:** use `HasAttributeAmong`, not the `.All()`-buggy
  `HasAttributesWithNames` overload (P5).

---

## Key references (so the next session doesn't re-discover them)

- Dispatch seam: `source/Sailfish/Execution/ClassExecutionDispatcher.cs`
- Engine + inline notifications: `source/Sailfish/Execution/SailfishExecutionEngine.cs`
- Raw samples: `source/Sailfish/Execution/PerformanceTimer.cs` (`ExecutionIterationPerformances`)
- IPC payload: `source/Sailfish/Contracts.Public/Serialization/Tracking.V1/` (`PerformanceRunResultTrackingFormat.RawExecutionResults`, `ITrackingFileSerialization`)
- Attribute to extend: `source/Sailfish/Attributes/SailfishAttribute.cs` (model on `SailfishLifetime.cs`)
- Settings: `source/Sailfish/Contracts.Public/Models/IRunSettings.cs`, `RunSettings.cs`, `RunSettingsBuilder.cs`, `SailfishPreset.cs`
- Adapter settings load: `source/Sailfish.TestAdapter/Execution/AdapterRunSettingsLoader.cs`, `.../TestSettingsParser/SettingsConfiguration.cs`
- DI: `AddSailfish` + `source/Sailfish/Registration/SailfishTypeRegistrationUtility.cs`
- Entry points: `source/Sailfish/SailfishRunner.cs`, `source/Sailfish.TestAdapter/TestExecutor.cs`

---

## Session log

| Date | Who | What changed |
|---|---|---|
| 2026-06-03 | Claude | Created project directory; wrote README/SPEC/PLAN/HANDOFF; opened planning PR. Confirmed (by reading `main`): in-process-only execution, dispatcher seam, raw samples in `PerformanceTimer`, and `Tracking.V1` carries `RawExecutionResults` (reusable as IPC payload). |
