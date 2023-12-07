using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldHavePublicSettersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.SailfishVariablesShouldHavePublicSettersDescriptor;
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

            var propertySymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, property);
            if (propertySymbol is null || propertySymbol.DeclaredAccessibility != Accessibility.Public) continue;

            var setterIsPublic = propertySymbol
                .ContainingType
                .GetMembers()
                .ToList()
                .OfType<IMethodSymbol>()
                .Where(m => m.Name.Equals($"set_{property.Identifier.Text}"))
                .SingleOrDefault(m => m.MethodKind == MethodKind.PropertySet)?
                .DeclaredAccessibility == Accessibility.Public;

            if (!setterIsPublic) context.ReportDiagnostic(Diagnostic.Create(Descriptor, property.Identifier.GetLocation(), property.Identifier.Text));
        }
    }
}