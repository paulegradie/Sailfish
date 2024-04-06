using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldHavePublicGettersAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1001",
        title: "Properties assigned in the global setup must have public getters",
        messageFormat: "Property '{0}' must have a public getter when assigned within a method decorated with the SailfishGlobalSetup attribute",
        AnalyzerGroups.EssentialAnalyzers.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        description: "Properties assigned in the global setup must have public getters.",
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

        foreach (var propertyInsideSetupMethod in thingsAssignedInsideOfTheGlobalSetupMethods)
        {
            if (!propertyInsideSetupMethod.IsClassPropertyOrField()) continue;
            if (propertyInsideSetupMethod.Parent is not AssignmentExpressionSyntax) continue;
            var symbol = context.SemanticModel.GetSymbolInfo(propertyInsideSetupMethod).Symbol;

            if (
                symbol is not IPropertySymbol propertySymbol ||
                !propertySymbol.DeclaredAccessibility.HasFlag(Accessibility.Public) ||
                (
                    propertySymbol.GetMethod is not null &&
                    propertySymbol.GetMethod.DeclaredAccessibility.HasFlag(Accessibility.Public)
                )
            )
                continue;

            var locationInsideOfMethod = propertyInsideSetupMethod.Identifier.GetLocation();

            var diagnostic = Diagnostic.Create(Descriptor, locationInsideOfMethod, propertyInsideSetupMethod.Identifier.Text);
            context.ReportDiagnostic(diagnostic);

            var propertyDeclaration = classDeclaration
                .Members
                .OfType<PropertyDeclarationSyntax>()
                .SingleOrDefault(p => p.Identifier.Text == symbol.Name);

            var locationInClass = propertyDeclaration?.Identifier.GetLocation();
            if (propertyDeclaration is null) continue;
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, locationInClass, propertyDeclaration.Identifier.Text));
        }
    }
}