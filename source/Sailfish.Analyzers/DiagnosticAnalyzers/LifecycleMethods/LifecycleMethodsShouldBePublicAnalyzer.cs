using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LifecycleMethodsShouldBePublicAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.LifecycleMethodsShouldBePublic;
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

        var nonPublicLifecycleMethods = GetLifecycleMethods(classDeclaration);
        foreach (var nonPublicLifecycleMethod in nonPublicLifecycleMethods)
        {
            var diagnostic = Diagnostic.Create(Descriptor, nonPublicLifecycleMethod.Identifier.GetLocation(), nonPublicLifecycleMethod.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static IEnumerable<MethodDeclarationSyntax> GetLifecycleMethods(TypeDeclarationSyntax classDeclaration)
    {
        return classDeclaration
            .Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method =>
                !method.Modifiers
                    .Any(SyntaxKind.PublicKeyword))
            .Where(method => method.HasAttributeAmong(LifecycleAttributes.Names));
    }
}