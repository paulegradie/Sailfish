using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Sailfish.Analyzers.DiagnosticAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SuppressNonNullablePropertiesWarningWhenSetInGlobalSetupMethod : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.SuppressNonNullablePropertiesNotSetRule;
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
            .SelectMany(m =>
                m.DescendantNodes().OfType<IdentifierNameSyntax>())
            .ToList();

        foreach (var property in thingsAssignedInsideOfTheGlobalSetupMethods)
        {
            if (!property.IsClassPropertyOrField()) continue;
            if (property.Parent is not AssignmentExpressionSyntax) continue;

            var symbol = context.SemanticModel.GetSymbolInfo(property).Symbol;

            var propertyDeclaration = classDeclaration
                .Members
                .OfType<PropertyDeclarationSyntax>()
                .SingleOrDefault(p =>
                    symbol is not null && p.Identifier.Text == symbol.Name);

            if (propertyDeclaration is null) continue;

            if (!IsWarningSuppressed(semanticModel, "CS8618"))
                // Actively suppress the warning by reporting a hidden diagnostic
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptor,
                    propertyDeclaration.GetLocation(),
                    propertyDeclaration.Identifier.Text));
        }
    }

    private static bool IsWarningSuppressed(SemanticModel semanticModel, string warningId)
    {
        // Get the compilation associated with the semantic model
        var compilation = semanticModel.Compilation;

        // Get the compilation options
        var compilationOptions = compilation.Options;

        // Check if the specific warning is suppressed in the compilation options
        return compilationOptions.SpecificDiagnosticOptions.TryGetValue(warningId, out var reportDiagnostic)
               && reportDiagnostic == ReportDiagnostic.Suppress;
    }
}