using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Collections.Immutable;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;

public class LifecycleMethodsShouldBePublicAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1020",
        title: "Sailfish lifecycle methods must be public",
        messageFormat: "Method '{0}' must be public",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "Sailfish lifecycle methods must be public.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
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