#!/usr/bin/env node
// Generates the "Copy for LLM" files served by the docs site:
//   public/llms-full.txt  — a concise, hand-curated API reference (the thing the "Copy for LLM"
//                           button copies). Deliberately NOT the whole doc site verbatim: it is
//                           the minimum an agent needs to write and run Sailfish benchmarks.
//   public/llms.txt       — a short index (https://llmstxt.org convention) linking every docs page.
//
// Runs before `npm run build` / `npm run dev` (see package.json pre* hooks) and the output is
// committed so it is reviewable. When the API changes, update REFERENCE below; when a docs page is
// added/removed/renamed, update DOC_SECTIONS to match the sidebar in src/components/Layout.jsx.

import { writeFileSync, mkdirSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = dirname(fileURLToPath(import.meta.url))
const PUBLIC = join(HERE, '..', 'public')
const SITE_URL = 'https://paulgradie.com/Sailfish'

// Sidebar mirror — used only to build the llms.txt index of links. (docs/4 release notes omitted.)
const DOC_SECTIONS = [
  {
    title: 'Introduction',
    pages: [
      ['docs/0/getting-started', 'Getting Started'],
      ['docs/0/quick-start', 'Quick Start'],
      ['docs/0/installation', 'Installation'],
      ['docs/0/essential-information', 'Essential Information'],
      ['docs/0/when-to-use-sailfish', 'When To Use Sailfish'],
      ['docs/0/sailfish-vs-benchmarkdotnet', 'Sailfish vs BenchmarkDotNet'],
      ['docs/0/license', 'License'],
    ],
  },
  {
    title: 'Sailfish Basics',
    pages: [
      ['docs/1/required-attributes', 'Required Attributes'],
      ['docs/1/method-comparisons', 'Method Comparisons'],
      ['docs/1/sailfish-variables', 'Variables'],
      ['docs/1/sailfish-test-lifecycle', 'The Test Lifecycle'],
      ['docs/1/test-dependencies', 'Test Dependencies'],
      ['docs/1/adaptive-sampling', 'Adaptive Sampling'],
      ['docs/1/presets', 'Configuration Recipes'],
      ['docs/1/iteration-tuning', 'Iteration Tuning'],
      ['docs/1/measurement-and-overhead', 'Measurement & Overhead'],
      ['docs/1/steady-state-warmup', 'Steady-State Warmup'],
      ['docs/1/confidence-intervals', 'Confidence Intervals'],
      ['docs/1/outlier-handling', 'Outlier Handling'],
      ['docs/1/environment-health', 'Environment Health Check'],
      ['docs/1/anti-dce', 'Anti-DCE'],
      ['docs/1/precision-time-budget', 'Precision Time Budget'],
      ['docs/1/reproducibility-manifest', 'Reproducibility Manifest'],
    ],
  },
  {
    title: 'Outputs',
    pages: [
      ['docs/1/console-output', 'Reading the Console Output'],
      ['docs/1/output-attributes', 'Output Attributes'],
      ['docs/1/markdown-output', 'Markdown Output'],
      ['docs/1/csv-output', 'CSV Output'],
    ],
  },
  {
    title: 'Features',
    pages: [
      ['docs/2/sailfish', 'Sailfish'],
      ['docs/2/saildiff', 'SailDiff'],
      ['docs/2/scalefish', 'ScaleFish'],
      ['docs/2/skipper', 'Skipper (AI)'],
      ['docs/2/trawl', 'Trawl (Load Testing)'],
    ],
  },
  {
    title: 'Advanced Sailfish',
    pages: [
      ['docs/3/extensibility', 'Extensibility'],
      ['docs/3/example-app', 'Example App'],
    ],
  },
]

// The curated reference. Concise on purpose (~7k tokens): the API surface an agent needs, not the
// prose docs. Every default here is taken from source (SailfishAttribute / SailfishMethodAttribute /
// RunSettingsBuilder). For the full prose docs, follow the page links in llms.txt.
const REFERENCE = `# Sailfish — LLM API Reference

Sailfish is a .NET performance-testing library. You write benchmarks like unit tests
(attribute-decorated classes and methods), run them either through an IDE test adapter
(\`dotnet test\`) or programmatically in-process, and get per-invocation timing
*distributions* plus statistical analysis. It ships net8.0+ assemblies (net9.0, net10.0).

This is a concise API reference. Full prose docs, one page per topic, are linked from
${SITE_URL}/llms.txt. When to reach for Sailfish vs BenchmarkDotNet: use Sailfish for
request-scoped code (EF Core / SQL queries, HTTP handlers) where you want the per-call
latency distribution and p95/p99 tail — that is Sailfish's native, default measurement.
Use BenchmarkDotNet for sub-microsecond hot-path code, where batched invocation is needed
to resolve nanosecond costs. Measured like-for-like the two engines agree to within ~0.5%.

## Install

\`\`\`bash
dotnet add package Sailfish              # the library (programmatic use)
dotnet add package Sailfish.TestAdapter  # optional: tests show up in the IDE Test Explorer / dotnet test
\`\`\`

## Writing a benchmark

A benchmark is a class marked \`[Sailfish]\` with one or more \`[SailfishMethod]\` methods.
The timed region is the body of a \`[SailfishMethod]\`. Setup/teardown hooks are not timed.

\`\`\`csharp
using Sailfish.Attributes;

[Sailfish(SampleSize = 100, NumWarmupIterations = 10)]
public class QueryBenchmarks
{
    private MyDbContext _db = null!;

    [SailfishGlobalSetup]              // once per class (see lifetimes below)
    public void GlobalSetup() => _db = BuildContext();

    [SailfishMethodSetup]             // before each [SailfishMethod]
    public void MethodSetup() { }

    [SailfishMethod]                  // a benchmarked method; may be async and take a CancellationToken
    public async Task RecentOrders(CancellationToken ct) =>
        await _db.Orders.Where(o => o.CustomerId == 57).Take(20).ToListAsync(ct);

    [SailfishMethodTeardown] public void MethodTeardown() { }
    [SailfishGlobalTeardown] public void GlobalTeardown() => _db.Dispose();
}
\`\`\`

### \`[Sailfish]\` (class attribute) properties and defaults

- \`SampleSize\` = 15 — number of measured (timed) iterations per method.
- \`NumWarmupIterations\` = 10 — untimed warmups before measuring (floor when steady-state warmup is on).
- \`Disabled\` = false — skip the whole class.
- \`Lifetime\` = \`SailfishLifetime.SharedInstance\` — one instance + one GlobalSetup per class
  (expensive setup runs once). \`PerCase\` = fresh instance and DI scope per test case (strict isolation).
- \`DisableOverheadEstimation\` = false — subtract measured harness overhead from each sample.
- \`DisableComparison\` = false — by default every \`[SailfishMethod]\` in the class joins one implicit
  comparison group (see Method comparisons). Set true to opt the class out.
- \`OperationsPerInvoke\` = 1 — if the method body does N operations, set N to report per-operation time.
- \`OutlierStrategy\` = \`RemoveUpper\` — outlier handling (\`None\` / \`RemoveUpper\` / \`RemoveLower\` / \`RemoveAll\`).
- \`ForceGcBetweenIterations\` = true — force a GC between iterations so collection pauses fall outside samples.
- \`ConfidenceLevel\` = 0.95.
- \`UseAdaptiveSampling\` = false — stop early once results are statistically stable. Companions:
  \`MinimumSampleSize\` = 10, \`MaximumSampleSize\` = 1000, \`TargetCoefficientOfVariation\` = 0.05.
- \`UseSteadyStateWarmup\` = false — warm up until per-iteration timing stops trending (JIT/OSR settled)
  instead of a fixed count. Companion: \`MaxWarmupIterations\` = 50.
- \`UseEnvironmentControl\` = false, \`PinToSingleCore\` = false — opt-in environment stabilization.

### Lifecycle attributes (all methods, run in this order)

\`[SailfishGlobalSetup]\` → (per method) \`[SailfishMethodSetup]\` → (per iteration)
\`[SailfishIterationSetup]\` → **\`[SailfishMethod]\` (timed)** → \`[SailfishIterationTeardown]\` →
\`[SailfishMethodTeardown]\` → \`[SailfishGlobalTeardown]\`. Any hook may be async and/or take a
\`CancellationToken\`.

### \`[SailfishMethod]\` (method attribute) properties

- \`Order\` = int.MaxValue — optional ordering.
- \`Disabled\` = false, \`DisableComplexity\` = false, \`DisableOverheadEstimation\` = false.
- \`ComparisonGroup\` = null — advanced: name a group so only same-named methods are compared.
- \`IsBaseline\` = false — mark the baseline of a comparison group (produces an N−1 table vs that method;
  at most one per group). With no baseline, every pair is compared (N×N).

### Parameterizing (variables)

Put a public settable property on the class and decorate it; Sailfish runs the whole class once per value.

\`\`\`csharp
[SailfishVariable(1, 10, 100)] public int N { get; set; }         // discrete values
// Overloads accept string[], double[], long[], decimal[]. A leading bool enables ScaleFish:
[SailfishVariable(true, 1, 10, 100)] public int Size { get; set; } // feed ScaleFish complexity estimation
[SailfishRangeVariable(start: 0, count: 5, step: 2)] public int R { get; set; } // 0,2,4,6,8
\`\`\`

### Output attributes (class-level)

\`[WriteToMarkdown]\` and \`[WriteToCsv]\` emit consolidated markdown / CSV reports for the run.

## Running

### Via the test adapter (IDE / dotnet test)

Install \`Sailfish.TestAdapter\`; benchmarks then appear like xUnit/NUnit tests.

\`\`\`bash
dotnet test
dotnet test --filter "FullyQualifiedName~QueryBenchmarks"   # standard filters work
\`\`\`

Configure with a \`.sailfish.json\` next to the test \`.csproj\` (any omitted setting uses its default):

\`\`\`json
{ "SailDiffSettings": { "TestType": "WilcoxonRankSumTest", "Alpha": 0.05 } }
\`\`\`

### Programmatically (in-process)

\`\`\`csharp
using Sailfish;

var settings = RunSettingsBuilder.CreateBuilder()
    .TestsFromAssembliesContaining(typeof(QueryBenchmarks))
    .ProvidersFromAssembliesContaining(typeof(QueryBenchmarks)) // IRegisterSailfishServices for DI
    .WithSailDiff()                 // statistical comparison + verdicts
    .WithScaleFish()                // complexity estimation
    .WithLocalOutputDirectory("performance_output")
    .Build();

var result = await SailfishRunner.Run(settings);
if (!result.IsValid) foreach (var ex in result.Exceptions!) Console.WriteLine(ex);
\`\`\`

Useful \`RunSettingsBuilder\` methods: \`WithTestNames(params string[])\`,
\`WithGlobalSampleSize(int)\`, \`WithGlobalNumWarmupIterations(int)\`,
\`WithGlobalAdaptiveSampling(double targetCv, int maxSampleSize)\`,
\`WithSailDiff()\` / \`WithSailDiff(SailDiffSettings)\`, \`WithScaleFish()\`, \`WithAiAnalysis()\`,
\`WithTrawl(TrawlSettings)\`, \`WithMinimumLogLevel(LogLevel)\`, \`CreateTrackingFiles(bool)\`,
\`WithLocalOutputDirectory(string)\`.

### Reading results programmatically

\`\`\`csharp
foreach (var summary in result.ExecutionSummaries)
    foreach (var tc in summary.GetSuccessfulTestCases())
    {
        var perf = tc.PerformanceRunResult!;
        double[] rawSamplesMs = perf.RawExecutionResults;   // per-invocation times (milliseconds), unfiltered
        double mean = perf.Mean, median = perf.Median, stdDev = perf.StdDev;
        // perf.DataWithOutliersRemoved, perf.ConfidenceIntervals, etc. are also available.
    }
\`\`\`

## Analysis features

- **SailDiff** — statistical comparison. Two uses: (1) method comparisons — every \`[SailfishMethod]\`
  in a class is compared automatically (Improved/Slower/Similar at α=0.05, BH-FDR adjusted, with a
  ratio + 95% CI effect size); the verdict is identical across IDE, console, markdown and CSV.
  (2) historical comparison — explicitly provide a previous run's tracking file
  (\`WithProvidedBeforeTrackingFile\` / \`.sailfish.json\`); Sailfish does not auto-compare to your last run.
- **ScaleFish** — complexity estimation. Fans a \`[SailfishVariable]\` (bool-enabled overload) across
  sizes and fits O(1)/O(n)/O(n log n)/O(n²)/… to the measurements. Enable with \`.WithScaleFish()\`.
- **Trawl** — load testing. Drives a benchmark as concurrent virtual users. Enable with \`.WithTrawl(...)\`
  or the builder's \`WithTrawlVirtualUsers\` / \`WithTrawlMaxDuration\`.
- **Skipper (AI)** — optional AI narrative explaining SailDiff/ScaleFish results via a configured
  transport. Enable with \`.WithAiAnalysis()\`.

## Outputs

Per run Sailfish can emit: the console table (descriptive stats + comparison verdicts), consolidated
markdown and CSV (via \`[WriteToMarkdown]\`/\`[WriteToCsv]\`), tracking files (raw results for later
historical comparison), and optional distribution plots. Duration display unit is milliseconds.
`

// ---- write llms-full.txt (the curated reference) ----
const full = REFERENCE.replace(/\n{3,}/g, '\n\n').trimEnd() + '\n'

// ---- write llms.txt (index of all docs pages) ----
let index = `# Sailfish\n\n`
index += `> A .NET performance-testing library: write benchmarks like unit tests, run them via an IDE test adapter or in-process, and get per-invocation timing distributions with statistical analysis (SailDiff), complexity estimation (ScaleFish), load testing (Trawl), and optional AI explanations (Skipper).\n\n`
index += `A concise, LLM-ready API reference is at [llms-full.txt](${SITE_URL}/llms-full.txt). Full prose documentation, page by page:\n\n`
for (const section of DOC_SECTIONS) {
  index += `## ${section.title}\n\n`
  for (const [slug, navTitle] of section.pages) index += `- [${navTitle}](${SITE_URL}/${slug})\n`
  index += '\n'
}

mkdirSync(PUBLIC, { recursive: true })
writeFileSync(join(PUBLIC, 'llms-full.txt'), full)
writeFileSync(join(PUBLIC, 'llms.txt'), index)

const kb = (s) => `${(Buffer.byteLength(s) / 1024).toFixed(1)} KB`
const approxTokens = Math.round(full.length / 4)
console.log(`generated public/llms-full.txt (${kb(full)}, ~${approxTokens} tokens) and public/llms.txt (${kb(index)})`)
