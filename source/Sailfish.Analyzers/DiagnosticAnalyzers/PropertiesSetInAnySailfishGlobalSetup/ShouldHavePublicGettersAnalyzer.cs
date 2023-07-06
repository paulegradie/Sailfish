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
public class ShouldHavePublicGettersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.PropertiesAssignedInGlobalSetupShouldHavePublicGettersDescriptor;
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context)
    {
        try
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
        catch (Exception ex)
        {
            var trace = string.Join("\n", ex.StackTrace);
            throw new SailfishAnalyzerException($"Unexpected exception ~ {ex.Message} - {trace}");
        }
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
            {
                continue;
            }

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