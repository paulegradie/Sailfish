using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldHavePublicSettersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.PropertiesAssignedInGlobalSetupShouldHavePublicSettersDescriptor;
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        if (!Debugger.IsAttached) context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            analyzeContext =>
                AnalyzeSyntaxNode((ClassDeclarationSyntax)analyzeContext.Node,
                    analyzeContext.SemanticModel,
                    analyzeContext),
            SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeSyntaxNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
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
            {
                continue;
            }

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