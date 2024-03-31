using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;

namespace Sailfish.Analyzers.DiagnosticAnalyzers;

public class SuppressNonNullablePropertiesWarningWhenSetInGlobalSetupMethod : AnalyzerBase<ClassDeclarationSyntax>
{
    private static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF7000",
        title: "Suppresses warnings when a non nullable property is set in the global setup method",
        messageFormat: "'{0}' should be suppressed",
        category: AnalyzerGroups.SuppressionAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.SuppressionAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Hidden,
        description: "",
        helpLinkUri: AnalyzerGroups.SuppressionAnalyzers.HelpLink
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
        var compilation = semanticModel.Compilation;
        var compilationOptions = compilation.Options;
        return compilationOptions.SpecificDiagnosticOptions.TryGetValue(warningId, out var reportDiagnostic) && reportDiagnostic == ReportDiagnostic.Suppress;
    }
}