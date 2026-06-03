using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The framework's default <see cref="ISkipperPromptBuilder" />. Assembles the prompt as three parts:
///     <list type="number">
///         <item>a framework-owned <b>grounding preamble</b> ("these numbers are authoritative, explain the why, cite
///         <c>file:line</c>") — always first;</item>
///         <item>every registered <see cref="ISkipperPromptSection" /> in ascending order (the framework's default
///         body sections plus any a consumer added);</item>
///         <item>a framework-owned <b>output-schema contract</b> — always last.</item>
///     </list>
///     The preamble and the output contract are emitted here, not as injectable sections, so they can never be
///     dropped or reordered. The output contract is the twin of <see cref="DefaultSkipperResponseParser" />: the
///     field names below ARE the schema that parser reads back. Change the two together; <c>SkipperOutputContractTests</c>
///     locks them in sync.
/// </summary>
internal sealed class DefaultSkipperPromptBuilder : ISkipperPromptBuilder
{
    private readonly IReadOnlyList<ISkipperPromptSection> sections;

    public DefaultSkipperPromptBuilder(IEnumerable<ISkipperPromptSection> sections)
    {
        // Stable ordering: ascending Order, ties broken by registration order (OrderBy is a stable sort).
        this.sections = sections.OrderBy(section => section.Order).ToArray();
    }

    public string Build(SkipperSession session)
    {
        var prompt = new StringBuilder();

        AppendPreamble(prompt, session);
        foreach (var section in sections)
        {
            section.Contribute(prompt, session);
        }

        AppendOutputContract(prompt);
        return prompt.ToString();
    }

    private static void AppendPreamble(StringBuilder prompt, SkipperSession session)
    {
        // Prefer the granted, scoped capability; fall back to the session's resolved root.
        var repositoryRoot = session.Capabilities.Get<ICodeReadCapability>()?.RepositoryRoot ?? session.RepositoryRoot;

        prompt.AppendLine(
            "You are Skipper, a performance-analysis agent embedded in the Sailfish benchmarking framework.");
        prompt.AppendLine($"Your role for this review is: {session.Role}.");
        prompt.AppendLine();
        prompt.AppendLine(
            "The benchmark numbers below are AUTHORITATIVE — do not recompute, contradict, or invent figures.");
        prompt.AppendLine(
            "Your mechanistic explanation MUST be consistent with the measured ordering: rank candidates by their");
        prompt.AppendLine(
            "means in the context, and never claim a candidate is faster than one the numbers show it is slower");
        prompt.AppendLine(
            "than, nor attribute a speed-up (e.g. SIMD/JIT intrinsics) to a candidate measured as slower.");
        prompt.AppendLine(
            "Your job is to EXPLAIN them: read the code under test to determine WHY performance changed or");
        prompt.AppendLine(
            "scales the way it does (allocations, query shape, a lost fast-path, etc.) and cite concrete");
        prompt.AppendLine("evidence as `relative/path.cs:line`.");
        prompt.AppendLine($"You have read-only access (Read/Grep/Glob) rooted at: {repositoryRoot}");
        prompt.AppendLine();
    }

    // === Output-schema contract — twin of DefaultSkipperResponseParser. Keep field names in sync. ===
    private static void AppendOutputContract(StringBuilder prompt)
    {
        prompt.AppendLine("## Required output");
        prompt.AppendLine(
            "After investigating the code, respond with EXACTLY ONE JSON object and nothing else");
        prompt.AppendLine("(no prose, no markdown code fences). Schema:");
        prompt.AppendLine();
        prompt.AppendLine("{");
        prompt.AppendLine("  \"overallVerdict\": \"Improved | Regressed | NotSignificant | Inconclusive\",");
        prompt.AppendLine("  \"consoleSummary\": \"<= 3 short lines stating the headline finding\",");
        prompt.AppendLine("  \"markdownReport\": \"detailed markdown; explain the WHY and cite code as path.cs:line\",");
        prompt.AppendLine("  \"findings\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"testCaseDisplayName\": \"one of the case names listed above\",");
        prompt.AppendLine("      \"verdict\": \"Improved | Regressed | NotSignificant | Inconclusive\",");
        prompt.AppendLine("      \"summary\": \"one-paragraph diagnosis\",");
        prompt.AppendLine("      \"citedSourceLocations\": [\"relative/path.cs:line\"],");
        prompt.AppendLine("      \"confidence\": 0.0");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");
    }
}
