# Next Agent Prompt — 2.3 (Tier B: Anti‑DCE Analyzers)

Goal: Implement SF1001–SF1003 analyzers (with code fixes), wire into Sailfish.Analyzers, add tests and docs.

## Read these first (absolute paths)
- G:/code/Sailfish/PHASE2_QUICK_START.md
- G:/code/Sailfish/AiAssistedDevSpecs/ImprovedRigor-1/AntiDCEAnalyzers-Design-v1.md
- G:/code/Sailfish/site/src/pages/docs/1/anti-dce.md

## Scope
- Add `source/Sailfish.Analyzers` (Roslyn analyzers) and `source/Tests.Analyzers`
- Implement SF1001–SF1003 per the design doc
- Ensure analyzers are included in the Sailfish NuGet package

## Steps
1) Scaffold projects
   - `dotnet new classlib -n Sailfish.Analyzers -f netstandard2.0`
   - `dotnet new xunit -n Tests.Analyzers -f net8.0`
   - Add Roslyn packages to analyzers project; reference analyzers from tests
2) Implement SF1001 + tests + code fix
3) Implement SF1002 + tests + code fix
4) Implement SF1003 + tests + code fix
5) Wire analyzers into packaging and CI (ensure tests run in solution)
6) Update `/site/src/pages/docs/1/anti-dce.md` with rules + examples; update release notes

## Verification
- Build: `dotnet build G:/code/Sailfish/source/Sailfish.sln -v:m`
- Unit tests: `dotnet test G:/code/Sailfish/source/Tests.Library/Tests.Library.csproj -c Debug -f net8.0 -m:1 -v:m`
- Analyzer tests: `dotnet test G:/code/Sailfish/source/Tests.Analyzers/Tests.Analyzers.csproj -c Debug -f net8.0 -m:1 -v:m`

## Acceptance Criteria
- All analyzer tests pass; solution builds
- Warnings surface only for intended scenarios in sample code
- Docs updated; release notes appended

## Notes
- Do not run demo PerformanceTests projects in CI
- Prefer minimal, deterministic tests
- Follow the design doc guidance on false positives and safe sinks

