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
public class OnlyOneLifecycleAttributePerMethod : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.OnlyOneLifecycleAttributePerMethod;
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

        var methodsWithLifecycleAttributes = classDeclaration
            .Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method => method.HasAttributeAmong(LifecycleAttributes.Names));

        foreach (var methodsWithLifecycleAttribute in methodsWithLifecycleAttributes)
        {
            var lifecycleAttributes = methodsWithLifecycleAttribute
                .AttributeLists
                .SelectMany(attributeListSyntax =>
                    attributeListSyntax
                        .Attributes
                        .Select(attributeSyntax =>
                            attributeSyntax.Name.ToString()))
                .Where(x => LifecycleAttributes.Names.Contains(x))
                .ToList();

            if (lifecycleAttributes.Count > 1)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, methodsWithLifecycleAttribute.Identifier.GetLocation(), methodsWithLifecycleAttribute.Identifier.Text));
        }
    }
}