using System.Linq;
using System.Text;
using static Sailfish.Analysis.Ai.SkipperNumberFormat;

namespace Sailfish.Analysis.Ai;

// The framework's default prompt body sections. Each renders one slice of the grounded
// PerformanceNarrativeContext into the prompt; the builder orders them by SkipperPromptOrder and emits the
// grounding preamble before and the output-schema contract after. A section emits nothing when its slice is
// empty, so a SailDiff-only run carries no scaling block and vice versa.

/// <summary>Renders the authoritative SailDiff before / after comparisons.</summary>
internal sealed class ComparisonsPromptSection : ISkipperPromptSection
{
    public int Order => SkipperPromptOrder.Comparisons;

    public void Contribute(StringBuilder prompt, SkipperSession session)
    {
        var comparisons = session.Context.Comparisons;
        if (comparisons is not { Count: > 0 })
        {
            return;
        }

        prompt.AppendLine("## SailDiff comparisons (authoritative)");
        foreach (var c in comparisons)
        {
            prompt.AppendLine($"- **{c.DisplayName}** — verdict: {c.Verdict}{(c.Failed ? " (test FAILED)" : "")}");
            prompt.AppendLine(
                $"  - mean: {Num(c.MeanBefore)} -> {Num(c.MeanAfter)} ({Signed(c.PercentChangeMean)}% mean change)");
            prompt.AppendLine($"  - median: {Num(c.MedianBefore)} -> {Num(c.MedianAfter)}");
            prompt.AppendLine(
                $"  - p-value: {Num(c.PValue)}{(c.AdjustedPValue is { } q ? $", adjusted (q): {Num(q)}" : "")}");
            if (c.EffectSizeName is { Length: > 0 })
            {
                prompt.AppendLine($"  - effect size ({c.EffectSizeName}): {Num(c.EffectSizeValue ?? double.NaN)}");
            }

            if (c.MinimumDetectableEffectPercent is { } mde)
            {
                prompt.AppendLine($"  - minimum detectable effect: {Num(mde)}%");
            }

            prompt.AppendLine($"  - samples (before/after): {c.SampleSizeBefore}/{c.SampleSizeAfter}");
            prompt.AppendLine($"  - change description: {c.ChangeDescription}");
        }

        prompt.AppendLine();
    }
}

/// <summary>Renders the authoritative ScaleFish complexity fits and their projections to larger N.</summary>
internal sealed class ScalingPromptSection : ISkipperPromptSection
{
    public int Order => SkipperPromptOrder.Scaling;

    public void Contribute(StringBuilder prompt, SkipperSession session)
    {
        var scaling = session.Context.Scaling;
        if (scaling is not { Count: > 0 })
        {
            return;
        }

        prompt.AppendLine("## ScaleFish complexity fits (authoritative)");
        foreach (var s in scaling)
        {
            prompt.AppendLine(
                $"- **{s.TestMethodName}** (variable `{s.PropertyName}`): best fit {s.BestFitComplexity} " +
                $"(goodness {Num(s.GoodnessOfFit)}); next best {s.NextBestComplexity} " +
                $"(goodness {Num(s.NextBestGoodnessOfFit)}); distinguishable: {s.IsDistinguishable}" +
                (s.SuggestedNextN is { } n ? $"; suggested next N: {n}" : ""));

            if (s.Projections is { Count: > 0 })
            {
                var projections = string.Join(", ", s.Projections.Select(p => $"N={p.N}:{Num(p.PredictedValue)}"));
                prompt.AppendLine($"  - projections: {projections}");
            }
        }

        prompt.AppendLine();
    }
}

/// <summary>Renders the environment snapshot and any reproducibility concerns the model should temper its verdict on.</summary>
internal sealed class EnvironmentPromptSection : ISkipperPromptSection
{
    public int Order => SkipperPromptOrder.Environment;

    public void Contribute(StringBuilder prompt, SkipperSession session)
    {
        var environment = session.Context.Environment;
        if (environment is null)
        {
            return;
        }

        prompt.AppendLine("## Environment & reproducibility");
        prompt.AppendLine(
            $"- runtime {environment.DotNetRuntime}, {environment.Os} ({environment.OsArchitecture}), " +
            $"process {environment.ProcessArchitecture}");
        if (environment.CpuModel is { Length: > 0 })
        {
            prompt.AppendLine($"- CPU: {environment.CpuModel}");
        }

        prompt.AppendLine(
            $"- GC: {environment.GcMode}, JIT: {environment.Jit}, timer: {environment.Timer}, " +
            $"affinity: {environment.CpuAffinity}");
        prompt.AppendLine($"- health score: {environment.HealthScore} ({environment.HealthLabel})");

        if (environment.Concerns is { Count: > 0 })
        {
            prompt.AppendLine("- health concerns (temper your confidence accordingly):");
            foreach (var concern in environment.Concerns)
            {
                prompt.AppendLine(
                    $"  - {concern.Name} [{concern.Status}]: {concern.Details}" +
                    (concern.Recommendation is { Length: > 0 } ? $" -> {concern.Recommendation}" : ""));
            }
        }

        prompt.AppendLine();
    }
}

/// <summary>Renders the verbatim SailDiff / ScaleFish result table — the authoritative units, exactly as printed.</summary>
internal sealed class ResultTablePromptSection : ISkipperPromptSection
{
    public int Order => SkipperPromptOrder.ResultTable;

    public void Contribute(StringBuilder prompt, SkipperSession session)
    {
        var markdown = session.Context.SailDiffMarkdown;
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return;
        }

        prompt.AppendLine("## Sailfish result table (verbatim — authoritative units)");
        prompt.AppendLine(markdown);
        prompt.AppendLine();
    }
}
