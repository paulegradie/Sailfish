# NextAgentPrompt-2.10 — Sailfish PR #213 Coverage Plan and Handoff

Goal: Achieve and verify ≥80% line coverage on all new files introduced in PR #213; ensure analyzer/code-fix tests compile and run; produce a per-file coverage summary for added/changed files in the PR.

Repo root: G:\code\Sailfish
Solution path: G:\code\Sailfish\source\Sailfish.sln
PR link: https://github.com/paulegradie/Sailfish/pull/213

## Current Status Snapshot
- All PR #213 review comments addressed previously.
- All test suites (Tests.Library, Tests.TestAdapter, Tests.Analyzers) were passing.
- EnvironmentHealthChecker.cs: 80.28% line coverage, ~73.33% branch coverage (target met for line coverage).
- Analyzer CodeFix tests exist but compilation is blocked by missing packages:
  - Microsoft.CodeAnalysis.CSharp.CodeFix.Testing
  - Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit

## Blocking Approval Needed
Per project rules, do not modify dependencies without permission.
Request permission to run these in G:\code\Sailfish\source\Tests.Analyzers:
- dotnet add package Microsoft.CodeAnalysis.CSharp.CodeFix.Testing --version 1.1.2
- dotnet add package Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit --version 1.1.2

Once approved, install, rebuild, and proceed with analyzer/code-fix coverage work.

## Acceptance Criteria
- All new files introduced by PR #213 reach ≥80% line coverage (per Cobertura reports).
- Tests pass on .NET 8 and .NET 9 targets used by the repo.
- Analyzer + CodeFix providers have meaningful test coverage (diagnostics and fixes, including Fix All where applicable).
- Golden tests (Markdown/CSV) remain green.
- Per-file coverage summary generated for PR #213 new/changed files, with any remaining below-80% items called out and assigned test scenarios.

## Key Paths
- Solution: G:\code\Sailfish\source\Sailfish.sln
- Tests.Analyzers project: G:\code\Sailfish\source\Tests.Analyzers\Tests.Analyzers.csproj
- Analyzer/CodeFix sources: G:\code\Sailfish\source\Sailfish.Analyzers\DiagnosticAnalyzers\PerformancePitfalls
  - ConstantOnlyComputationAnalyzer.cs
  - ConstantOnlyComputationCodeFixProvider.cs
  - EmptyLoopBodyAnalyzer.cs
  - EmptyLoopBodyCodeFixProvider.cs
  - UnusedReturnValueAnalyzer.cs
  - UnusedReturnValueCodeFixProvider.cs
- Analyzer tests: G:\code\Sailfish\source\Tests.Analyzers\PerformancePitfalls
  - ConstantOnlyComputation.cs (analyzer tests)
  - ConstantOnlyComputationCodeFixTests.cs (code-fix tests)
  - EmptyLoopBody.cs (analyzer tests)
  - EmptyLoopBodyCodeFixTests.cs (code-fix tests)
  - UnusedReturnValue.cs (analyzer tests)
  - UnusedReturnValueCodeFixTests.cs (code-fix tests)
- Library tests for EnvironmentHealthChecker: G:\code\Sailfish\source\Tests.Library\Diagnostics\Environment\EnvironmentHealthCheckerTests.cs
- Coverage outputs (by default): under G:\code\Sailfish\source\<project>\TestResults\<GUID>\coverage.cobertura.xml

## How to Run (Verification + Coverage)
1) Open terminal and verify working directory:
   - cd /d G:\code\Sailfish\source
   - cd   (confirm PWD output)

2) Build solution:
   - dotnet build Sailfish.sln -c Debug -v:m -nologo

3) Run tests with coverage per project (avoid solution-level quoting pitfalls):
   - dotnet test Tests.Analyzers/Tests.Analyzers.csproj -c Debug -v:minimal -m:1 --nologo --collect "XPlat Code Coverage"
   - dotnet test Tests.Library/Tests.Library.csproj -c Debug -v:minimal -m:1 --nologo --collect "XPlat Code Coverage"
   - dotnet test Tests.TestAdapter/Tests.TestAdapter.csproj -c Debug -v:minimal -m:1 --nologo --collect "XPlat Code Coverage"

Coverage reports: check TestResults directories for coverage.cobertura.xml.

Note: CodeFix tests won’t compile until the two Roslyn testing packages are installed in Tests.Analyzers.

## Systematic Plan
A) Unblock CodeFix test harness (permission required)
- Install packages listed above.
- Rebuild Tests.Analyzers; fix any minor namespace/usings mismatches in test files if they arise.

B) Complete CodeFix test suites to raise coverage
1. UnusedReturnValueCodeFixProvider
   - Scenario: wraps ignored invocation result with Consumer.Consume(…) and adds using Sailfish.Utilities if missing.
   - Multi-diagnostic case (two ignored invocations in a method) + Fix All verification.
2. ConstantOnlyComputationCodeFixProvider
   - Scenario: constant-only expression gets Consumer.Consume((expr)). Include const fields treated as constants (not external input).
   - Multiple constant expressions in a method + Fix All.
3. EmptyLoopBodyCodeFixProvider
   - Scenarios: for/while/foreach empty bodies; foreach with var and with deconstruction; embedded statements vs. braced; insert Consumer.Consume(0).
   - Multi-loop Fix All.

C) Analyzer coverage top-ups (only if <80%)
- ConstantOnlyComputationAnalyzer: nested constant exprs; const fields; negative cases that should not report.
- EmptyLoopBodyAnalyzer: while (;), foreach deconstruction, single-statement loops with semicolon, foreach variable.
- UnusedReturnValueAnalyzer: ignored results of method calls vs. assigned to local; chained invocations; property/field access sinks not flagged.

D) EnvironmentHealthChecker optional branch improvements (line coverage already ≥80%)
- Exception paths in CheckBuildConfiguration/CheckJitSettings/CheckGcMode/CheckTimerResolution.
- Process priority: test all priority classes and exception path.
- CPU affinity: zero mask; unsupported OS branch guards.
- Power scheme: macOS/Linux branches; TryGetActivePowerScheme timeout and malformed outputs.
- Background CPU load: varying thresholds and exception path coverage.

E) Generate per-file coverage summary for PR #213
- Re-run coverage (see commands above).
- Locate all coverage.cobertura.xml files under:
  - G:\code\Sailfish\source\Tests.Analyzers\TestResults
  - G:\code\Sailfish\source\Tests.Library\TestResults
- Parse Cobertura XML to collect per-file line-rate and branch-rate.
- Get PR #213 file list via GitHub API and cross-reference:
  - Endpoint: GET /repos/paulegradie/Sailfish/pulls/213/files
  - Focus on entries with status = "added" (new files) and relevant code files in source/.
- Produce a CSV/markdown table: file path | line coverage % | branch coverage % | uncovered lines.
- Prioritize files below 80% line coverage and create concrete test scenarios for each.

## Minimal Test Examples (snippets)
1) UnusedReturnValue CodeFix — add using + wrap call
Before:
using Sailfish; class C { [SailfishMethod] void M(){ Do(); } }
After:
using Sailfish; using Sailfish.Utilities; class C { [SailfishMethod] void M(){ Consumer.Consume(Do()); } }

2) ConstantOnlyComputation CodeFix — wrap constant-only expression
Before:
[SailfishMethod] void M(){ var x = 1 + 2; }
After:
[SailfishMethod] void M(){ var x = Consumer.Consume(1 + 2); }

3) EmptyLoopBody CodeFix — insert consume for empty body
Before:
for (int i=0;i<10;i++);
After:
for (int i=0;i<10;i++){ Sailfish.Utilities.Consumer.Consume(0); }

Note: actual tests should use the Roslyn Verify test harness (CSharpCodeFixTest) with VerifyCodeFixAsync before/after strings and diagnostic expectations; include Fix All where applicable.

## Files Recently Added/Edited (focus areas)
- Analyzer/CodeFix source changes:
  - source/Sailfish.Analyzers/DiagnosticAnalyzers/PerformancePitfalls/ConstantOnlyComputationAnalyzer.cs
  - source/Sailfish.Analyzers/DiagnosticAnalyzers/PerformancePitfalls/ConstantOnlyComputationCodeFixProvider.cs
  - source/Sailfish.Analyzers/DiagnosticAnalyzers/PerformancePitfalls/EmptyLoopBodyAnalyzer.cs
  - source/Sailfish.Analyzers/DiagnosticAnalyzers/PerformancePitfalls/EmptyLoopBodyCodeFixProvider.cs
  - source/Sailfish.Analyzers/DiagnosticAnalyzers/PerformancePitfalls/UnusedReturnValueAnalyzer.cs
  - source/Sailfish.Analyzers/DiagnosticAnalyzers/PerformancePitfalls/UnusedReturnValueCodeFixProvider.cs
- Analyzer tests created/updated:
  - source/Tests.Analyzers/PerformancePitfalls/ConstantOnlyComputationCodeFixTests.cs
  - source/Tests.Analyzers/PerformancePitfalls/EmptyLoopBodyCodeFixTests.cs
  - source/Tests.Analyzers/PerformancePitfalls/UnusedReturnValueCodeFixTests.cs
- Golden tests touched:
  - source/Tests.Library/Presentation/MarkdownOutputGoldenTests.cs
  - source/Tests.Library/Presentation/CsvOutputGoldenTests.cs
- Environment health coverage push:
  - source/Tests.Library/Diagnostics/Environment/EnvironmentHealthCheckerTests.cs

## Guardrails and Notes
- .NET 8 and .NET 9 are supported; .NET 6 is deprecated.
- Analyzers: Console usage is banned (RS1035). Use analyzer reporting mechanisms.
- Keep golden test outputs culture-stable.
- Prefer smallest-scope, fast validation runs; do not install packages or deploy without explicit permission.

## Hand-off Checklist
- [ ] Obtain permission and install the two Roslyn CodeFix testing packages in Tests.Analyzers.
- [ ] Build solution; confirm Tests.Analyzers compiles.
- [ ] Run Tests.Analyzers with coverage; ensure CodeFix providers’ coverage increases.
- [ ] Run Tests.Library and Tests.TestAdapter with coverage; ensure no regressions.
- [ ] Generate per-file coverage table for PR #213 new/changed files via GitHub API + Cobertura cross-reference.
- [ ] For any file <80% line coverage, add targeted tests, re-run coverage, and iterate.
- [ ] Summarize final coverage and open any follow-ups for residual gaps (with scenarios and file paths).

## Quick Commands (copy/paste)
cd /d G:\code\Sailfish\source
cd

dotnet build Sailfish.sln -c Debug -v:m -nologo

dotnet test Tests.Analyzers/Tests.Analyzers.csproj -c Debug -v:minimal -m:1 --nologo --collect "XPlat Code Coverage"
dotnet test Tests.Library/Tests.Library.csproj   -c Debug -v:minimal -m:1 --nologo --collect "XPlat Code Coverage"
dotnet test Tests.TestAdapter/Tests.TestAdapter.csproj -c Debug -v:minimal -m:1 --nologo --collect "XPlat Code Coverage"

# After permission granted (install packages):
cd /d G:\code\Sailfish\source\Tests.Analyzers
cd

dotnet add package Microsoft.CodeAnalysis.CSharp.CodeFix.Testing --version 1.1.2
dotnet add package Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit --version 1.1.2

# Rebuild and re-run Tests.Analyzers with coverage afterwards

