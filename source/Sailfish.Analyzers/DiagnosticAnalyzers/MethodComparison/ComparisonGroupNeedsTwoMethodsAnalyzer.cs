using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;

/// <summary>
/// SF1302 — A ComparisonGroup with only one method has nothing to compare against and
/// produces no comparison output. The user probably meant to add a second member, or
/// remove the ComparisonGroup if the method shouldn't participate.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ComparisonGroupNeedsTwoMethodsAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1302",
        "ComparisonGroup needs at least two methods",
        "ComparisonGroup '{0}' has only one method ('{1}'); add another method to the group or remove the ComparisonGroup",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Warning,
        description: "A comparison group with fewer than two methods produces no output; either add a peer or drop the group.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var infos = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(MethodComparisonAttributeReader.TryRead)
            .Where(info => info is not null && !string.IsNullOrEmpty(info.ComparisonGroup))
            .Cast<MethodComparisonInfo>();

        foreach (var group in infos.GroupBy(i => i.ComparisonGroup!))
        {
            var members = group.ToList();
            if (members.Count >= 2) continue;

            var only = members[0];
            var location = only.ComparisonGroupArgument?.GetLocation() ?? only.Attribute.GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, group.Key, only.Method.Identifier.Text));
        }
    }
}
