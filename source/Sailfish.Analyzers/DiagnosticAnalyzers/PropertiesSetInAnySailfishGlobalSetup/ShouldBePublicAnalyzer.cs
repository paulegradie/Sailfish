using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Linq;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldBePublicAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1000",
        "Properties initialized in the global setup must be public",
        "Property '{0}' must be public when assigned within a method decorated with the SailfishGlobalSetup attribute",
        AnalyzerGroups.EssentialAnalyzers.Category,
        DiagnosticSeverity.Error,
        true,
        "Properties that are assigned in the SailfishGlobalSetup must be public.",
        $"{AnalyzerGroups.EssentialAnalyzers.HelpLink}");

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

        foreach (var propertyInsideOfSetupMethod in thingsAssignedInsideOfTheGlobalSetupMethods
                     .Where(propertyInsideOfSetupMethod => propertyInsideOfSetupMethod.IsClassPropertyOrField()))
        {
            if (propertyInsideOfSetupMethod.Parent is not AssignmentExpressionSyntax) continue;

            var symbol = context.SemanticModel.GetSymbolInfo(propertyInsideOfSetupMethod).Symbol;

            // No Non-Public
            if (symbol is not IPropertySymbol publicPropertySymbol || publicPropertySymbol.DeclaredAccessibility.HasFlag(Accessibility.Public)) continue;

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, propertyInsideOfSetupMethod.Identifier.GetLocation(), propertyInsideOfSetupMethod.Identifier.Text));

            var propertyDeclaration = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(p => p.Identifier.Text == symbol.Name);
            if (propertyDeclaration is not null)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, propertyDeclaration.Identifier.GetLocation(), propertyDeclaration.Identifier.Text));
        }
    }
}