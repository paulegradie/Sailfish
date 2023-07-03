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
public class ShouldHavePublicGettersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.SailfishVariablesShouldHavePublicGettersDescriptor;
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

        var properties = classDeclaration
            .Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(prop => prop.IsSailfishVariableProperty())
            .ToList();

        foreach (var property in properties)
        {
            var isSailfishVariableProperty = property
                .DescendantNodes()
                .OfType<AttributeSyntax>().Any(a => a.Name.ToString() == "SailfishVariable");
            if (!isSailfishVariableProperty) continue;

            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(property);
            if (propertySymbol is null) continue;

            var getterIsPublic = propertySymbol
                .ContainingType
                .GetMembers()
                .ToList()
                .OfType<IMethodSymbol>()
                .Where(m => m.Name.Equals($"get_{property.Identifier.Text}"))
                .SingleOrDefault(m => m.MethodKind == MethodKind.PropertyGet)?
                .DeclaredAccessibility == Accessibility.Public;

            if (!getterIsPublic)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, property.Identifier.GetLocation(), property.Identifier.Text));
            }
        }
    }
}