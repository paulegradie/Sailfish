using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldBePublicAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1010",
        "Properties decorated with the SailfishVariableAttribute must be public",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        messageFormat: "Property '{0}' must be public",
        defaultSeverity: DiagnosticSeverity.Error,
        description: "Properties decorated with the SailfishVariableAttribute must be public.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var nonPublicProperties = GetNonPublicProperties(classDeclaration);
        foreach (var property in nonPublicProperties)
        {
            if (!property.IsSailfishVariableProperty()) continue;
            var diagnostic = Diagnostic.Create(Descriptor, property.Identifier.GetLocation(), property.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static IEnumerable<PropertyDeclarationSyntax> GetNonPublicProperties(TypeDeclarationSyntax classDeclaration)
    {
        return classDeclaration
            .Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(property =>
                !property.Modifiers
                    .Any(SyntaxKind.PublicKeyword));
    }
}