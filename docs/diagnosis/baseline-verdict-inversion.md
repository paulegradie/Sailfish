# Diagnosis: inverted baseline verdicts in the IDE comparison / SailDiff output

**Surfaced on:** Sailfish.TestAdapter 4.0.136
**Status:** **FIXED in this PR.** Root cause below; the regression test (which failed against the
pre-fix code) now passes, and the fix is verified end-to-end with a real RELEASE run (§6).

---

## 1. Summary

When a `[SailfishMethod(ComparisonGroup = "…", IsBaseline = true)]` comparison group is run
through the **TestAdapter**, the IDE "IMPACT" verdict lines (and the detailed table, console
table, CSV, and distribution-plot caption built from the same data) come out wrong in two ways:

1. **Wrong baseline identity.** The method flagged `IsBaseline = true` is *not* used as the
   baseline. The IDE comparison path never reads `IsBaseline` at all — it emits an
   order-dependent N×N matrix and labels one side of each pair "baseline."
2. **Inverted / transposed verdict.** For half the emitted lines the named method and its printed
   mean are transposed, so the slowest method is reported as the fastest.

Both are reproduced deterministically (no real timing) by
[`MethodComparisonBatchProcessor_BaselineInversionTests`](../../source/Tests.TestAdapter/Queue/MethodComparisonBatchProcessor_BaselineInversionTests.cs),
which drives the **real** formatter through the production `MethodComparisonBatchProcessor` and
**fails** against current code. A live (disabled-by-default) benchmark repro is at
[`BaselineVerdictInversionRepro`](../../source/PerformanceTests/ExamplePerformanceTests/BaselineVerdictInversionRepro.cs).

### Captured evidence (from the failing test, synthetic means)

Ground truth fed in: `WithPlus` (the `IsBaseline` method) is the **slowest**
(`N:100 → 8 ms`, `N:10000 → 240 ms`); `WithStringBuilder` is the **fastest**
(`N:100 → 1 ms`, `N:10000 → 4 ms`).

`WithStringBuilder`'s test row prints (WRONG — the reported symptom):

```
🟢 IMPACT: WithPlus(N: 10000) is 99.6% faster than baseline WithStringBuilder(N: 100) (IMPROVED)
   P-Value: 0.000000 | Mean: 240.000 ms → 1.000 ms
```

`240 ms` is `WithPlus`'s mean but is printed on the `baseline WithStringBuilder` side; `1 ms` is
`WithStringBuilder`'s mean but printed as the `WithPlus` value. `WithPlus` is reported "faster"
when it is ~160× **slower**. This is identical in shape to the field report
(`WithPlus(N: 1000) is 99.4% faster than baseline WithStringBuilder(N: 100) | Mean: 178.815 µs → 1.102 µs`).

`WithPlus`'s own test row prints the *same* pair correctly:

```
🟢 IMPACT: WithStringBuilder(N: 100) is 87.5% faster than baseline WithPlus(N: 100) (IMPROVED)
   P-Value: 0.000000 | Mean: 8.000 ms → 1.000 ms
```

The "only rows whose **candidate** is the baseline-flagged method invert" pattern from the field
report is confirmed and explained in §3.3.

### Real-timing confirmation (RELEASE run through the live TestAdapter)

The same defect reproduces with genuine measurements. Running
[`BaselineVerdictInversionRepro`](../../source/PerformanceTests/ExamplePerformanceTests/BaselineVerdictInversionRepro.cs)
in `-c Release` through the TestAdapter, `WithStringBuilder(N: 100)`'s row prints:

```
🟢 IMPACT: WithPlus(N: 10000) is 100.0% faster than baseline WithStringBuilder(N: 100) (IMPROVED)
   P-Value: 0.000000 | Mean: 18.966 ms → 0.001 ms
```

`18.966 ms` is `WithPlus(N: 10000)`'s real O(n²) mean, printed on the `baseline WithStringBuilder`
side; `0.001 ms` (StringBuilder) is printed as the `WithPlus` value. The slowest method is reported
"faster" — the exact field symptom, just at different N/magnitude. The correct counterpart on
`WithPlus`'s own row reads `WithStringBuilder(N: 100) is 80.7% faster than baseline WithPlus(N: 100)`.
The run also emitted cross-N rows such as
`WithPlus(N: 100) is 850.2% slower than baseline WithStringBuilder(N: 10000)` (see §5.3).

---

## 2. The two responsible mechanisms

### 2.1 Symptom 2 (inversion/transposition): a dead perspective-swap guard

There is exactly **one** producer of perspective-based comparison data:
`MethodComparisonBatchProcessor.FormatComparisonResults`
([`MethodComparisonBatchProcessor.cs:454-475`](../../source/Sailfish.TestAdapter/Queue/Processors/MethodComparison/MethodComparisonBatchProcessor.cs)).
For one SailDiff pair (`before = methodA`, `after = methodB`) it builds **two**
`SailDiffComparisonData` objects, one per "perspective":

```csharp
var result = comparisonResult.SailDiffResults.First();
var isBeforePerspective = perspectiveMethodName == beforeMethodName;
var primaryMethod  = ExtractMethodName(perspectiveMethodName);                                 // leaf name
var comparedMethod = ExtractMethodName(isBeforePerspective ? afterMethodName : beforeMethodName);

var comparisonData = new SailDiffComparisonData
{
    GroupName            = groupName,
    PrimaryMethodName    = primaryMethod,     // the perspective method is ALWAYS named the "baseline"
    ComparedMethodName   = comparedMethod,
    Statistics           = result.TestResultsWithOutlierAnalysis.StatisticalTestResult, // MeanBefore=mean(A), MeanAfter=mean(B)
    …
    IsPerspectiveBased   = true,
    PerspectiveMethodName = perspectiveMethodName    // a FULLY-QUALIFIED TestCaseId ("Ns.Class.M(N: x)")
};
```

Every consumer then decides whether to swap before/after with the **same predicate**:

```csharp
// ImpactSummaryFormatter.AnalyzeComparison (lines 55-60) — the IMPACT verdict line
var primaryTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName
    ? display.MeanAfter
    : display.MeanBefore;
var comparedTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName
    ? display.MeanBefore
    : display.MeanAfter;
```

The guard `PerspectiveMethodName == ComparedMethodName` is **dead** — it can never be true — for
two independent reasons:

- **(D1) Wrong role.** The producer always assigns the perspective method to `PrimaryMethodName`
  (`primaryMethod = ExtractMethodName(perspectiveMethodName)`). The perspective method is therefore
  **never** the `ComparedMethodName`. The intent was "swap when this row is rendered from the
  *after* method's perspective," i.e. `perspectiveMethodName == afterMethodName` — but the code
  compares against the *compared* method instead.
- **(D2) Key/format mismatch.** `PerspectiveMethodName` is a **fully-qualified** TestCaseId
  (`"SailfishBugRepro.BaselineVerdictInversionRepro.WithStringBuilder(N: 100)"`), while
  `ComparedMethodName` is the **leaf** name produced by `ExtractMethodName`
  (`"WithPlus(N: 100)"`). A full-id-vs-leaf compare cannot match even if D1 were fixed. This is the
  "TestCaseId not fully qualified on both sides" keying collision hypothesized in the brief: the
  branch that *should* realign the operands is silently skipped, so the after-perspective operands
  stay transposed relative to the labels.

Because the guard is always false, **every** consumer uses `primaryTime = MeanBefore`,
`comparedTime = MeanAfter` regardless of perspective:

| Perspective | Verdict baseline (`PrimaryMethodName`) | `primaryTime` used | Correct? |
|---|---|---|---|
| before (methodA's row) | A | `MeanBefore` = mean(A) | ✅ name ↔ mean aligned |
| after  (methodB's row) | B | `MeanBefore` = mean(A) | ❌ baseline named **B** but shows mean(**A**) |

The after-perspective row is exactly the symptom: baseline named B carries A's mean, candidate
named A carries B's mean, and the direction (`(comparedTime-primaryTime)/primaryTime`) is computed
on the still-A/B times, so it points the wrong way relative to the swapped names.

### 2.2 Symptom 1 (wrong baseline identity): `IsBaseline` is never consulted

The TestAdapter comparison path contains **zero** references to `IsBaseline`
(`grep -r IsBaseline source/Sailfish.TestAdapter` → no hits). Pairing is a pure, order-dependent
N×N over every case in the batch
([`MethodComparisonBatchProcessor.cs:145-162`](../../source/Sailfish.TestAdapter/Queue/Processors/MethodComparison/MethodComparisonBatchProcessor.cs)):

```csharp
for (var i = 0; i < testCases.Count; i++)
for (var j = i + 1; j < testCases.Count; j++)
{
    var methodA = testCases[i];   // treated as "before"
    var methodB = testCases[j];   // treated as "after"
    …
    await PerformMethodComparison(methodA, methodB, groupName, cancellationToken);
}
```

The word "baseline" in the IDE output is therefore just the `PrimaryMethodName` = the perspective
method of whichever row you're reading — never the `IsBaseline`-flagged method. In the captured run
`WithStringBuilder`'s rows even label `WithStringBuilder` as the baseline.

> Contrast: the **markdown** path (`MethodComparisonTestRunCompletedHandler.CreateBaselineComparisonTable`,
> [lines 348-372 / 616-689](../../source/Sailfish/DefaultHandlers/Sailfish/MethodComparisonTestRunCompletedHandler.cs))
> *does* resolve the baseline from `IsBaseline` and keys the baseline case with `ReferenceEquals`
> (no name collision). That path is correct for the single-baseline case and is **not** affected by
> this bug — which is why the regression only shows up in the IDE / TestAdapter output.

---

## 3. Confirmation that this is the path (not a hash collision)

### 3.1 The IDE `🟢 IMPACT … | Mean: A → B` format is `ImpactSummaryFormatter.CreateIdeImpactSummary`

`ImpactSummaryFormatter.BuildVerdict`
([ImpactSummaryFormatter.cs:154-160](../../source/Sailfish/Analysis/SailDiff/Formatting/ImpactSummaryFormatter.cs))
emits `"{ComparedMethodName} is {pct}% {faster|slower} than baseline {PrimaryMethodName} ({SIG})"`,
and `CreateIdeImpactSummary` appends `"   P-Value: … | Mean: {primaryTime} → {comparedTime}"`. This
matches the field report byte-for-byte.

### 3.2 Operand mapping is faithful

`AdapterSailDiff.ComputeTestCaseDiff`
([AdapterSailDiff.cs:88-106](../../source/Sailfish.TestAdapter/Execution/AdapterSailDiff.cs)) sets
`before = preloadedLastRun` (= `methodA`) and `after = the summary result` (= `methodB`), so
`MeanBefore = mean(methodA)`, `MeanAfter = mean(methodB)`. The regression test's fake
`IAdapterSailDiff` mirrors this exactly, so the means the real formatter sees are production-faithful.

### 3.3 Why "only candidate == baseline-method rows invert"

Discovery emits a comparison group's cases method-by-method, so all `WithPlus` cases precede all
`WithStringBuilder` cases in the batch. With the `i < j` loop, **every mixed pair has the baseline
method (`WithPlus`) as `methodA` (= before)**. Hence:

- the **before** perspective (a `WithPlus` row) is correct: baseline = `WithPlus`, candidate = `WithStringBuilder`;
- the **after** perspective (a `WithStringBuilder` row) is the transposed one, and its
  `ComparedMethodName` is the before method = `WithPlus`.

So every inverted mixed line has **candidate == the baseline-flagged method** — exactly the
field-report correlation. It is a side-effect of ordering + the dead guard, not an explicit (mis)lookup.

---

## 4. Blast radius

The dead guard `IsPerspectiveBased && PerspectiveMethodName == ComparedMethodName` is duplicated
across **five** formatter files (every surface that renders a perspective comparison transposes the
after-perspective identically):

| File | Lines | Surface |
|---|---|---|
| `ImpactSummaryFormatter.cs` | 55, 58 | IDE/console/markdown IMPACT verdict + `Mean: A → B` |
| `SailDiffUnifiedFormatter.cs` | 163, 166 | `SailDiffFormattedOutput.PercentageChange` / significance |
| `DetailedTableFormatter.cs` | 97, 165, 215, 240, 243, 246, 249 | IDE / Markdown / Console / CSV detail tables |
| `DistributionPlotFormatter.cs` | 87 | distribution-plot mean/median caption |

The single producer is `MethodComparisonBatchProcessor.FormatComparisonResults`
(`IsPerspectiveBased = true` at line 473). Any caller that constructs `SailDiffComparisonData` with
`IsPerspectiveBased = false` (e.g. `SailDiffDataExtensions.ToComparisonData`) is unaffected — the
guard short-circuits on `IsPerspectiveBased`.

---

## 5. The fix (applied in this PR)

### 5.1 Operand assignment + keying (symptom 2)

The producer now **pre-orients** the statistics. `MethodComparisonBatchProcessor.FormatOriented`
(replacing `FormatComparisonResults`) hands the formatters a `StatisticalTestResult` whose
before/after sides are swapped (via `OrientStatistics`) whenever the named baseline was the SailDiff
"after" operand — so `MeanBefore`/`RawDataBefore` **always** describe `PrimaryMethodName`. The
dead `IsPerspectiveBased && PerspectiveMethodName == ComparedMethodName` guard is removed from all
five consumer files (`ImpactSummaryFormatter`, `SailDiffUnifiedFormatter`, `DetailedTableFormatter`,
`DistributionPlotFormatter`); they now use `primary = MeanBefore`, `compared = MeanAfter`
unconditionally. The fragile full-id-vs-leaf-name compare is gone.

### 5.2 Honor `IsBaseline` (symptom 1)

`IsBaseline` is now plumbed through discovery: `DiscoveryAnalysisMethods.ExtractIsBaseline` detects
the attribute argument, `MethodMetaData.IsBaseline` carries it, and `TestCaseItemCreator` sets the
existing `SailfishComparisonRoleProperty = "Baseline"` (the message mappers already forward it to
`metadata["ComparisonRole"]`). `MethodComparisonBatchProcessor` reads that role and, when a cohort
has exactly one baseline, compares every contender **against it** (baseline-vs-contender), so the
flagged method is always named the baseline. With no flagged baseline it falls back to N×N — now
internally consistent thanks to §5.1.

### 5.3 Cross-N grouping

`ProcessComparisonGroup` partitions each comparison group by **variable set** (`ExtractVariableSection`)
before pairing, so contenders are only compared to the baseline **at the same N**. The markdown path
(`MethodComparisonTestRunCompletedHandler`) does the same via `RenderComparisonCohort` +
`GetVariableSection`, which also lets a scaled baseline resolve to exactly one case per size instead
of tripping the old ">1 baseline" N×N fallback. Single-variable-set groups render exactly as before.

---

## 6. Regression test + verification

[`source/Tests.TestAdapter/Queue/MethodComparisonBatchProcessor_BaselineInversionTests.cs`](../../source/Tests.TestAdapter/Queue/MethodComparisonBatchProcessor_BaselineInversionTests.cs)

- **`IdeComparisonOutput_DoesNotInvertOrMislabelTheBaseline`** — drives the production
  `MethodComparisonBatchProcessor.ProcessBatch` with the **real** `SailDiffUnifiedFormatter` and a
  faithful fake `IAdapterSailDiff`. Feeds synthetic per-case stats (baseline = large mean,
  candidate = small mean) across **two** `SailfishVariable` values, captures the published IDE
  output, and asserts on every parsed verdict line: (a) for mixed pairs the baseline named is the
  `IsBaseline` method (`WithPlus`); (b) the smaller-mean method is reported "faster"; (c) no line
  pairs a case against itself or transposes the printed means.
- **`ImpactSummary_RendersPreOrientedComparison_WithoutTransposing`** — locks the post-fix formatter
  contract: the formatter renders pre-oriented data verbatim (baseline carries MeanBefore) and never
  re-derives or swaps. A regression here would resurrect the inverted verdict.

Both **failed** against the pre-fix code (symptom output captured in §1); both **pass** now.

**End-to-end RELEASE verification.** Running the (normally `Disabled`) benchmark
[`BaselineVerdictInversionRepro`](../../source/PerformanceTests/ExamplePerformanceTests/BaselineVerdictInversionRepro.cs)
through the live TestAdapter with the fix emits only correct, same-N verdicts:

```
🟢 IMPACT: WithStringBuilder(N: 100)   is 79.2% faster than baseline WithPlus(N: 100)   (IMPROVED)
🟢 IMPACT: WithStringBuilder(N: 10000) is 99.8% faster than baseline WithPlus(N: 10000) (IMPROVED)
```

— baseline correctly resolved to the `IsBaseline` method `WithPlus` (validating the discovery→role
plumbing), correct direction, and no cross-N rows (compare with the 12 inverted/cross-N lines in §1).
