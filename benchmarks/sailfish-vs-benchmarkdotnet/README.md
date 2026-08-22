# Sailfish vs BenchmarkDotNet — comparison harness

This folder contains the reproducible experiment behind the docs page
[Sailfish vs BenchmarkDotNet](../../site/src/pages/docs/0/sailfish-vs-benchmarkdotnet.md)
(published at `/docs/0/sailfish-vs-benchmarkdotnet`), which itself is the supporting
evidence for [When To Use Sailfish](../../site/src/pages/docs/0/when-to-use-sailfish.md).

Three workloads are implemented **once**, as static methods in a shared assembly, and
measured by both tools so that any difference in the numbers comes from measurement
methodology — not the code under test:

| Workload | What it is | True scale |
| --- | --- | --- |
| `EfCoreQuery` | EF Core LINQ query (filter + order + take 20) against seeded in-memory SQLite, 5,000 rows | ~0.1 ms |
| `CpuHash` | SHA-256 over a fixed 64 KB buffer | ~20 µs |
| `TinyOp` | Sum of a 256-element int array | ~70 ns |

## Layout

- `Workloads/` — the shared code under test (EF Core + SQLite fixture included). No harness references.
- `SailfishRun/` — console runner: Sailfish via project reference to `../../source/Sailfish`,
  `SampleSize = 10000`. Writes `samples_sailfish.csv` (raw `RawExecutionResults`, ms → ns).
- `BdnRun/` — console runner: BenchmarkDotNet with two jobs — stock **BDN-Default** and
  **BDN-PerInvocation** (`InvocationCount = 1, UnrollFactor = 1, IterationCount = 10000`),
  the like-for-like bridge to Sailfish's one-sample-per-call geometry. Parses BDN's
  `*-report-full.json` and writes `samples_bdn.csv` (workload-actual iterations, ns ÷ ops).
- `analysis/merge_csvs.py` — merges the two CSVs into `samples.json` and prints summary stats.
- `analysis/make_svgs.py` — renders the three violin-plot SVGs used by the docs site
  (written to `site/public/benchmark-comparison/` by default).
- `data/samples-10k-2026-08-22.json` — the merged raw samples from the original run
  (macOS / Apple Silicon, .NET 9.0.9, BDN 0.15.4), so the plots can be regenerated
  without re-running the experiment.

The two runners are deliberately **separate processes**: BenchmarkDotNet pins
`Perfolizer 0.5.3` exactly while Sailfish requires `>= 0.7.1`, so the two tools cannot
be referenced from a single project (NU1107). These projects are intentionally not part
of `Sailfish.sln`.

## Reproducing the experiment

Run each suite (Release; run them sequentially so they don't contend for CPU):

```bash
cd benchmarks/sailfish-vs-benchmarkdotnet
dotnet run --project SailfishRun -c Release -- ./compare-output
dotnet run --project BdnRun -c Release -- ./compare-output
```

Expected runtime at n = 10,000 on Apple Silicon: ~35 s for the Sailfish suite,
~2 min for the BDN suite. Then merge and regenerate the docs-site plots:

```bash
python3 analysis/merge_csvs.py ./compare-output
python3 analysis/make_svgs.py ./compare-output/samples.json
```

If the numbers change materially (new hardware, new runtime, engine changes), update the
statistics quoted on the docs page to match — the page states its environment explicitly.

## Notes for future runs

- Quiet the machine: close heavy apps; on laptops, run on mains power.
- The EF workload is **non-stationary**: it keeps warming for thousands of invocations
  (caches, JIT). Per-call medians therefore depend on warm-up policy and sample count —
  hold both constant when comparing runs.
- `TinyOp` needs thousands of invocations before the JIT fully tiers up; small sample
  sizes will report cold-code numbers (this is visible as multiple lobes in its violins).
- BDN's full JSON report at n = 10,000 is ~14 MB; it is parsed, not committed.
