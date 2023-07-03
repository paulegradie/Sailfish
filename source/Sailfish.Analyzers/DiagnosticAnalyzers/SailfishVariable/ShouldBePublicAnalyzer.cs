using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldBePublicAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Descriptor = Descriptors.SailfishVariablesShouldBePublicDescriptor;
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