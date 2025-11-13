# NextAgentPrompt-2.5 — Seeded randomized run order (Tier A)

Implement deterministic, seeded run ordering for test methods within comparison groups to improve reproducibility and UX.

## Required reading (absolute paths)
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/Sailfish_iPhone_Level_Polish_PRD.md (Section G. Seeded randomized run order)
- G:/code/Sailfish/source/Sailfish/RunSettingsBuilder.cs
- G:/code/Sailfish/source/Sailfish/Contracts.Public/Models/IRunSettings.cs
- G:/code/Sailfish/source/Sailfish/Results/ReproducibilityManifest.cs
- G:/code/Sailfish/site/src/pages/docs/1/reproducibility-manifest.md
- G:/code/Sailfish/site/src/pages/docs/1/markdown-output.md

## Scope
- Add a nullable Seed to run settings; when set, order test execution deterministically by seed.
- Persist the seed in the Reproducibility Manifest; show it in the consolidated markdown header.
- Provide a clear re-run command that includes the seed (optional nice-to-have if plumbing exists already).

## Design sketch
- API: extend IRunSettings with `int? Seed { get; }` and RunSettingsBuilder with `.WithSeed(int seed)`.
- Ordering: at the point where test cases are grouped for execution/comparison, shuffle deterministically using the provided seed. If null, keep current behavior.
- Manifest: add `Seed` field to the run/session section.
- Markdown: add a one-liner in the session header like “Seed: 123456”.

## Proposed file touchpoints
- source/Sailfish/Contracts.Public/Models/IRunSettings.cs (+ Seed)
- source/Sailfish/RunSettingsBuilder.cs (+ WithSeed)
- source/Sailfish/Results/ReproducibilityManifest.cs (+ Seed)
- source/Sailfish/Execution/[Scheduler|Orderer].cs (wherever run order is produced)
- source/Sailfish/Presentation/MarkdownTableConverter.cs (append Seed in header)
- Tests in source/Tests.Library and/or Tests.TestAdapter to prove order determinism and manifest/markdown updates

## Steps
1) Add Seed to settings and builder (default null; no behavior change when null)
2) Introduce a deterministic shuffle utility (e.g., Fisher–Yates with `Random(seed)`)
3) Apply the orderer at the earliest point where per-group case lists are finalized
4) Persist Seed to manifest; render in markdown header
5) Tests:
   - Unit: Given a fixed seed and list, ordering is stable; different seeds yield different orders
   - Adapter/library: Seed appears in manifest JSON and in markdown header when enabled

## Acceptance criteria
- All existing tests pass on net8.0 (and keep net9.0 compatibility)
- With a fixed seed, run order is deterministic across runs and processes
- Seed is persisted in the manifest and displayed in the markdown header
- Setting is opt-in (null by default) and does not affect prior behavior
- Docs updated: reproducibility-manifest.md (add Seed), markdown-output.md (mention Seed)

## Verification commands (safe)
- dotnet build G:/code/Sailfish/source/Sailfish.sln -v:m
- dotnet test G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj -c Debug -f net8.0 -m:1 -v:m
- Optionally: add a focused test in Tests.TestAdapter and run it similarly

## Notes
- Keep the change minimal and internal to the scheduler/ordering logic; avoid coupling to unrelated systems.
- Prefer deterministic, culture-invariant behavior when sorting before shuffling.
- If no clean scheduler point exists yet, add a small, well-named component (e.g., `SeededOrderer`) and call it where cases are batched.

