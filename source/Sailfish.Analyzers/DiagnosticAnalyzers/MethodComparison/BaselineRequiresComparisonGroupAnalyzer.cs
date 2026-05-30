using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;

/// <summary>
/// SF1300 — <c>IsBaseline = true</c> is only meaningful when the method participates in some
/// comparison group. With class-default comparison on, this is satisfied either by an explicit
/// <c>ComparisonGroup</c> on the method or by the enclosing <c>[Sailfish]</c> class not setting
/// <c>DisableComparison = true</c>. The analyzer fires when both are missing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaselineRequiresComparisonGroupAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1300",
        "IsBaseline=true on a method that isn't in any comparison group",
        "Method '{0}' sets IsBaseline = true but isn't in any comparison group. Either set ComparisonGroup on the method, or remove DisableComparison = true from the class's [Sailfish] attribute.",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "IsBaseline only makes sense within a comparison group; a method outside every group cannot be a baseline.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var classDisables = classDeclaration is ClassDeclarationSyntax cds && MethodComparisonAttributeReader.ClassDisablesComparison(cds);

        foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            var info = MethodComparisonAttributeReader.TryRead(method);
            if (info is null) continue;
            if (!info.IsBaseline) continue;

            // Method participates in some comparison group when either:
            //   - It has an explicit ComparisonGroup, OR
            //   - The class allows the implicit class-wide group (DisableComparison = false).
            if (!string.IsNullOrEmpty(info.ComparisonGroup)) continue;
            if (!classDisables) continue;

            var location = info.IsBaselineArgument?.GetLocation() ?? info.Attribute.GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, method.Identifier.Text));
        }
    }
}
