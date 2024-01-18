using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldBePublicAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.PropertiesAssignedInGlobalSetupShouldBePublicDescriptor;
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

        foreach (var propertyInsideOfSetupMethod in thingsAssignedInsideOfTheGlobalSetupMethods)
        {
            if (!propertyInsideOfSetupMethod.IsClassPropertyOrField()) continue;
            if (propertyInsideOfSetupMethod.Parent is not AssignmentExpressionSyntax) continue;

            var symbol = context.SemanticModel.GetSymbolInfo(propertyInsideOfSetupMethod).Symbol;

            // No Non Public
            if (symbol is not IPropertySymbol publicPropertySymbol || publicPropertySymbol.DeclaredAccessibility.HasFlag(Accessibility.Public)) continue;

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, propertyInsideOfSetupMethod.Identifier.GetLocation(), propertyInsideOfSetupMethod.Identifier.Text));

            var propertyDeclaration = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(p => p.Identifier.Text == symbol.Name);
            if (propertyDeclaration is not null)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, propertyDeclaration.Identifier.GetLocation(), propertyDeclaration.Identifier.Text));
        }
    }
}