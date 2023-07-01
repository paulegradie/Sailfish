using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.DiagnosticAnalyzers.Utils;
using Sailfish.Analyzers.DiagnosticAnalyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SailfishVariablesShouldBePublicAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor SailfishVariablesShouldBePublicDescriptor = DescriptorHelper.CreateDescriptor(
        AnalyzerGroups.EssentialAnalyzers,
        1000,
        isEnabledByDefault: true,
        title: "SailfishVariable properties should be public",
        description: "SailfishVariables are get and set using by the test framework and should therefore should be public",
        severity: DiagnosticSeverity.Error);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SailfishVariablesShouldBePublicDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        if (!Debugger.IsAttached) context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(analyzeContext => AnalyzeSyntaxNode((ClassDeclarationSyntax)analyzeContext.Node, analyzeContext.SemanticModel, analyzeContext),
            SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeSyntaxNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.HasSailfishAttribute(semanticModel)) return;

        var nonPublicProperties = GetNonPublicProperties(classDeclaration);
        foreach (var property in nonPublicProperties)
        {
            if (!property.IsSailfishVariableProperty(context)) continue;
            var diagnostic = Diagnostic.Create(SailfishVariablesShouldBePublicDescriptor, property.Identifier.GetLocation(), property.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static IEnumerable<PropertyDeclarationSyntax> GetNonPublicProperties(TypeDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members.OfType<PropertyDeclarationSyntax>().Where(property => !property.Modifiers.Any(SyntaxKind.PublicKeyword));
    }
}