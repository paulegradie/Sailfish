using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldHavePublicSettersAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1015",
        "Properties assigned in the global setup must have public setters",
        "Property '{0}' must have a public setter when assigned within a method decorated with the SailfishGlobalSetup attribute",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "Properties assigned in the global setup must have public setters.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var globalSetupMethods = classDeclaration
            .Members
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.HasAttributesWithNames("SailfishGlobalSetup"))
            .ToList();

        var thingsAssignedInsideOfTheGlobalSetupMethods = globalSetupMethods
            .SelectMany(m => m.DescendantNodes().OfType<IdentifierNameSyntax>())
            .ToList();

        foreach (var propertyInsideOfSetupMethod in thingsAssignedInsideOfTheGlobalSetupMethods)
        {
            if (!propertyInsideOfSetupMethod.IsClassPropertyOrField()) continue;
            if (propertyInsideOfSetupMethod.Parent is not AssignmentExpressionSyntax) continue;

            var symbol = context.SemanticModel.GetSymbolInfo(propertyInsideOfSetupMethod).Symbol;

            if (
                symbol is not IPropertySymbol propertySymbol ||
                !propertySymbol.DeclaredAccessibility.HasFlag(Accessibility.Public) ||
                (
                    propertySymbol.SetMethod is not null &&
                    propertySymbol.SetMethod.DeclaredAccessibility.HasFlag(Accessibility.Public)
                )
            )
                continue;

            var locationInsideOfMethod = propertyInsideOfSetupMethod.Identifier.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, locationInsideOfMethod, propertyInsideOfSetupMethod.Identifier.Text));

            var propertyDeclaration = classDeclaration
                .Members
                .OfType<PropertyDeclarationSyntax>()
                .SingleOrDefault(p => p.Identifier.Text == symbol.Name);
            var location = propertyDeclaration?.Identifier.GetLocation();

            if (propertyDeclaration is null) continue;
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, propertyDeclaration.Identifier.Text));
        }
    }
}