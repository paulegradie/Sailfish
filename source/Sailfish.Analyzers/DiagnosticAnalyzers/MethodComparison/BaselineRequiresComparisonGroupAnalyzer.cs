using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;

/// <summary>
/// SF1300 — When a method sets <c>IsBaseline = true</c>, it must also set
/// <c>ComparisonGroup</c>. A baseline outside a group is meaningless.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaselineRequiresComparisonGroupAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1300",
        "IsBaseline=true requires a ComparisonGroup",
        "Method '{0}' sets IsBaseline = true but does not specify a ComparisonGroup. Add 'ComparisonGroup = \"...\"' to the SailfishMethod attribute, or remove IsBaseline.",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "IsBaseline only makes sense within a named ComparisonGroup; declaring a baseline without one is invalid.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            var info = MethodComparisonAttributeReader.TryRead(method);
            if (info is null) continue;
            if (!info.IsBaseline) continue;
            if (!string.IsNullOrEmpty(info.ComparisonGroup)) continue;

            // Report at the IsBaseline argument if we can locate it; otherwise at the attribute.
            var location = info.IsBaselineArgument?.GetLocation() ?? info.Attribute.GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, method.Identifier.Text));
        }
    }
}
