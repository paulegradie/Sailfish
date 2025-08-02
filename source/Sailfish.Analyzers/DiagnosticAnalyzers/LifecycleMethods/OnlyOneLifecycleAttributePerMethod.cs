using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Linq;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.LifecycleMethods;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OnlyOneLifecycleAttributePerMethod : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1021",
        "Only one Sailfish lifecycle attribute is allowed per method",
        "Method '{0}' may only be decorated with a single Sailfish lifecycle attribute",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "Only one Sailfish lifecycle attribute is allowed per method.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
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