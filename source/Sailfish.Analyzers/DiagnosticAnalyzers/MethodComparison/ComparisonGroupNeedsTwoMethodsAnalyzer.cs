using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;

/// <summary>
/// SF1302 — A comparison group with only one method (including the implicit class-wide group)
/// has nothing to compare against and produces no comparison output. The user probably meant to
/// add a peer, drop the explicit <c>ComparisonGroup</c>, or opt the class out via
/// <c>DisableComparison = true</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ComparisonGroupNeedsTwoMethodsAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1302",
        "Comparison group needs at least two methods",
        "Comparison group {0} has only one method ('{1}') — add another method or remove the group",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Warning,
        description: "A comparison group with fewer than two methods produces no output; either add a peer, drop the explicit group, or set DisableComparison = true on the class.",
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
            var members = group.ToList();
            if (members.Count >= 2) continue;

            var only = members[0].Info;
            var displayGroupKey = group.Key == MethodComparisonInfo.ImplicitGroupKey
                ? "(implicit class-wide)"
                : $"'{group.Key}'";

            // Anchor the diagnostic on the most informative location: the ComparisonGroup arg if explicit;
            // otherwise the SailfishMethod attribute itself (which is where the implicit group is "declared").
            var location = only.ComparisonGroupArgument?.GetLocation() ?? only.Attribute.GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, displayGroupKey, only.Method.Identifier.Text));
        }
    }
}
