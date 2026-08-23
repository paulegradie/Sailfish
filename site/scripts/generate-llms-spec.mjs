#!/usr/bin/env node
// Generates the "Copy for LLM" spec served by the docs site:
//   public/llms-full.txt  — the entire docs corpus as one LLM-ready markdown document
//   public/llms.txt       — a short index (https://llmstxt.org convention) linking each page
//
// Runs automatically before `npm run build` / `npm run dev` (see package.json pre* hooks).
// The generated files are also committed so they are reviewable and present regardless of
// how the site is built. Keep DOC_SECTIONS in sync with the sidebar in src/components/Layout.jsx.

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = dirname(fileURLToPath(import.meta.url))
const SITE = join(HERE, '..')
const PAGES = join(SITE, 'src', 'pages')
const PUBLIC = join(SITE, 'public')
const SITE_URL = 'https://paulgradie.com/Sailfish'

// Ordered to mirror the docs sidebar. releasenotes (docs/4) is intentionally omitted:
// a changelog is noise for an agent learning how to use the library.
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

// A hand-written, authoritative quick reference placed at the very top of the full spec so an
// agent is immediately productive without having to infer the API from prose.
const PREAMBLE = `# Sailfish — LLM Reference

> Sailfish is a .NET performance-testing library. You write benchmarks like unit tests
> (attribute-decorated classes/methods), run them in-process or via an IDE test adapter,
> and get per-invocation timing distributions plus statistical analysis (SailDiff),
> complexity estimation (ScaleFish), load testing (Trawl), and optional AI explanations
> (Skipper). This document is the complete documentation concatenated for LLM consumption.
> Target framework support: net8.0+ (the library ships net9.0 and net10.0 assemblies).

## Quick reference

Install:

\`\`\`bash
dotnet add package Sailfish
# For the IDE test adapter (tests appear like xUnit/NUnit in the Test Explorer):
dotnet add package Sailfish.TestAdapter
\`\`\`

Minimal benchmark class:

\`\`\`csharp
using Sailfish.Attributes;

[Sailfish(SampleSize = 100, NumWarmupIterations = 10)]
public class MyBenchmark
{
    [SailfishGlobalSetup]        // runs once per class (SharedInstance lifetime, the default)
    public void GlobalSetup() { /* expensive setup: build DI, seed a DB, etc. */ }

    [SailfishMethodSetup]        // runs before each method
    public void MethodSetup() { }

    [SailfishMethod]             // a benchmarked method; timed region is the method body
    public async Task MyScenario(CancellationToken ct) { /* code under test */ }

    [SailfishMethodTeardown] public void MethodTeardown() { }
    [SailfishGlobalTeardown] public void GlobalTeardown() { }
}
\`\`\`

Key attributes and defaults:

- \`[Sailfish(...)]\` marks a benchmark class. Common properties:
  \`SampleSize\` (measured iterations), \`NumWarmupIterations\` (default 10),
  \`Disabled\`, \`DisableOverheadEstimation\`, \`DisableComparison\`,
  \`Lifetime\` (\`SailfishLifetime.SharedInstance\` default, or \`PerCase\`),
  \`UseAdaptiveSampling\` (default false; with \`MinimumSampleSize\`/\`MaximumSampleSize\`/\`TargetCoefficientOfVariation\`),
  \`UseSteadyStateWarmup\` (default false; with \`MaxWarmupIterations\`),
  \`OutlierStrategy\` (default \`RemoveUpper\`), \`OperationsPerInvoke\` (default 1),
  \`ConfidenceLevel\` (default 0.95).
- \`[SailfishMethod]\` marks a benchmarked method. It may take a \`CancellationToken\` and be async.
- Lifecycle: \`[SailfishGlobalSetup]\`, \`[SailfishMethodSetup]\`, \`[SailfishIterationSetup]\`,
  \`[SailfishIterationTeardown]\`, \`[SailfishMethodTeardown]\`, \`[SailfishGlobalTeardown]\`.
- Parameterization: \`[SailfishVariable(1, 10, 100)]\` on a public property fans the test across values;
  \`[SailfishRangeVariable(...)]\` generates a range.
- Output: \`[WriteToMarkdown]\`, \`[WriteToCsv]\` on the class.

Running programmatically (in-process):

\`\`\`csharp
using Sailfish;

var settings = RunSettingsBuilder.CreateBuilder()
    .TestsFromAssembliesContaining(typeof(MyBenchmark))
    .ProvidersFromAssembliesContaining(typeof(MyBenchmark))
    .WithSailDiff()      // statistical before/after + method comparisons
    .WithScaleFish()     // complexity estimation
    .WithLocalOutputDirectory("performance_output")
    .Build();

var result = await SailfishRunner.Run(settings);
// result.IsValid; result.Exceptions; result.ExecutionSummaries
// Per-invocation raw samples (milliseconds):
//   summary.GetSuccessfulTestCases() -> tc.PerformanceRunResult.RawExecutionResults
\`\`\`

Running via the test adapter: install \`Sailfish.TestAdapter\`, then \`dotnet test\` (or use the IDE
Test Explorer). Filters work: \`dotnet test --filter "FullyQualifiedName~MyBenchmark"\`.
Configure via a \`.sailfish.json\` next to the test \`.csproj\`.

Choosing Sailfish vs BenchmarkDotNet: use Sailfish for request-scoped code (DB/EF Core queries,
API handlers) where you want the per-call latency distribution and p95/p99 tail — that is
Sailfish's native, default measurement. Use BenchmarkDotNet for sub-microsecond hot-path code,
where batched invocation is required to resolve nanosecond costs. Measured like-for-like the two
engines agree to within ~0.5%.

---

`

function stripFrontmatter(md) {
  const m = md.match(/^---\n([\s\S]*?)\n---\n?/)
  if (!m) return { title: null, body: md }
  const titleMatch = m[1].match(/title:\s*['"]?(.*?)['"]?\s*$/m)
  return { title: titleMatch ? titleMatch[1] : null, body: md.slice(m[0].length) }
}

// Convert the site's Markdoc tags to plain markdown that reads cleanly as text.
function markdocToMarkdown(md) {
  return md
    // {% terminal title="X" %}\n```...```\n{% /terminal %} -> keep the fenced block, title as a comment
    .replace(/\{%\s*terminal[^%]*%\}\s*/g, '')
    .replace(/\s*\{%\s*\/terminal\s*%\}/g, '')
    // {% callout title="T" type="X" %} body {% /callout %} -> **T (note)**\n body
    .replace(/\{%\s*callout\s+title="([^"]*)"[^%]*%\}/g, '\n**$1**\n')
    .replace(/\{%\s*\/callout\s*%\}/g, '\n')
    // {% figure src="s" alt="a" caption="c" /%} -> [Figure: c (s)]
    .replace(/\{%\s*figure\s+([^%]*?)\/%\}/g, (_, attrs) => {
      const cap = attrs.match(/caption="([^"]*)"/)
      const src = attrs.match(/src="([^"]*)"/)
      return `\n[Figure: ${cap ? cap[1] : ''}${src ? ` — ${SITE_URL}${src[1]}` : ''}]\n`
    })
    // drop quick-link navigation cards (home page only)
    .replace(/\{%\s*quick-links\s*%\}[\s\S]*?\{%\s*\/quick-links\s*%\}/g, '')
    .replace(/\{%[^%]*%\}/g, '') // any residual tags
    .replace(/\n{3,}/g, '\n\n')
    .trim()
}

function readPage(slug) {
  const raw = readFileSync(join(PAGES, `${slug}.md`), 'utf8')
  const { title, body } = stripFrontmatter(raw)
  return { title, body: markdocToMarkdown(body) }
}

// ---- build llms-full.txt ----
let full = PREAMBLE
for (const section of DOC_SECTIONS) {
  full += `\n# ${section.title}\n`
  for (const [slug, navTitle] of section.pages) {
    const { title, body } = readPage(slug)
    full += `\n## ${title || navTitle}\n\nSource: ${SITE_URL}/${slug}\n\n${body}\n`
  }
}
full = full.replace(/\n{3,}/g, '\n\n').trimEnd() + '\n'

// ---- build llms.txt (index) ----
let index = `# Sailfish\n\n`
index += `> A .NET performance-testing library: write benchmarks like unit tests, run them in-process or via an IDE test adapter, and get per-invocation timing distributions with statistical analysis (SailDiff), complexity estimation (ScaleFish), load testing (Trawl), and optional AI explanations (Skipper).\n\n`
index += `The complete documentation as a single file for LLM ingestion: [llms-full.txt](${SITE_URL}/llms-full.txt)\n\n`
for (const section of DOC_SECTIONS) {
  index += `## ${section.title}\n\n`
  for (const [slug, navTitle] of section.pages) {
    index += `- [${navTitle}](${SITE_URL}/${slug})\n`
  }
  index += '\n'
}

mkdirSync(PUBLIC, { recursive: true })
writeFileSync(join(PUBLIC, 'llms-full.txt'), full)
writeFileSync(join(PUBLIC, 'llms.txt'), index)

const kb = (s) => `${(Buffer.byteLength(s) / 1024).toFixed(1)} KB`
console.log(`generated public/llms-full.txt (${kb(full)}) and public/llms.txt (${kb(index)})`)
