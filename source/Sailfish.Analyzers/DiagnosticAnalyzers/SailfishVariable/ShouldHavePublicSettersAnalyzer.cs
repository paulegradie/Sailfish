using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariable;

public class ShouldHavePublicSettersAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1012",
        title: "Properties decorated with the SailfishVariableAttribute must have public setters",
        messageFormat: "Property '{0}' setter must be public",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "Properties decorated with the SailfishVariableAttribute must have public setters.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var properties = classDeclaration
            .Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(prop => prop.IsSailfishVariableProperty())
            .ToList();

        foreach (var property in properties)
        {
            var isSailfishVariableProperty = property
                .DescendantNodes()
                .OfType<AttributeSyntax>().Any(a => a.Name.ToString() == "SailfishVariable");
            if (!isSailfishVariableProperty) continue;

            var propertySymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, property);
            if (propertySymbol is null || propertySymbol.DeclaredAccessibility != Accessibility.Public) continue;

            var setterIsPublic = propertySymbol
                .ContainingType
                .GetMembers()
                .ToList()
                .OfType<IMethodSymbol>()
                .Where(m => m.Name.Equals($"set_{property.Identifier.Text}"))
                .SingleOrDefault(m => m.MethodKind == MethodKind.PropertySet)?
                .DeclaredAccessibility == Accessibility.Public;

            if (!setterIsPublic) context.ReportDiagnostic(Diagnostic.Create(Descriptor, property.Identifier.GetLocation(), property.Identifier.Text));
        }
    }
}