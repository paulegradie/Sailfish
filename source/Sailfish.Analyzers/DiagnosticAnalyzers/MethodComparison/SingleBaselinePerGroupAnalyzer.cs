using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;

/// <summary>
/// SF1301 — At most one method per comparison group (including the implicit class-wide group)
/// may set <c>IsBaseline = true</c>. When multiple are present the runtime falls back to N×N
/// comparison and emits a warning; the user almost certainly meant to pick one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SingleBaselinePerGroupAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1301",
        "Only one IsBaseline per comparison group is allowed",
        "Comparison group {0} has {1} methods marked IsBaseline = true; exactly one is allowed. The runtime will fall back to N×N comparison.",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "A comparison group can have zero baselines (N×N matrix) or exactly one baseline (N−1 comparisons). Two or more baselines is ambiguous.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var classDisables = classDeclaration is ClassDeclarationSyntax cds && MethodComparisonAttributeReader.ClassDisablesComparison(cds);

        var grouped = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(MethodComparisonAttributeReader.TryRead)
            .Where(info => info is not null)
            .Cast<MethodComparisonInfo>()
            .Select(info => new { Info = info, Key = info.EffectiveGroupKey(classDisables) })
            .Where(x => x.Key is not null)
            .GroupBy(x => x.Key);

        foreach (var group in grouped)
        {
            var baselines = group.Where(x => x.Info.IsBaseline).Select(x => x.Info).ToList();
            if (baselines.Count < 2) continue;

            var displayGroupKey = group.Key == MethodComparisonInfo.ImplicitGroupKey
                ? "(implicit class-wide)"
                : $"'{group.Key}'";

            foreach (var b in baselines)
            {
                var location = b.IsBaselineArgument?.GetLocation() ?? b.Attribute.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, displayGroupKey, baselines.Count));
            }
        }
    }
}
