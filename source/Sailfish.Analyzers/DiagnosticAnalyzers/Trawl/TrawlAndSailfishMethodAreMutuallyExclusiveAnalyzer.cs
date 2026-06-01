using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.Trawl;

/// <summary>
///     SF1022 — a method may not be decorated with both <c>[SailfishMethod]</c> and <c>[Trawl]</c>.
///     <c>[SailfishMethod]</c> runs a method as a sequential microbenchmark; <c>[Trawl]</c> runs it as a
///     concurrent load scenario. The two execution modes are mutually exclusive.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TrawlAndSailfishMethodAreMutuallyExclusiveAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    private static readonly string[] SailfishMethodNames = { "SailfishMethod", "SailfishMethodAttribute" };
    private static readonly string[] TrawlNames = { "Trawl", "TrawlAttribute" };

    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1022",
        "A method may not be both a microbenchmark and a load scenario",
        "Method '{0}' is decorated with both [SailfishMethod] and [Trawl]; a method is either a microbenchmark or a load scenario, not both",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Error,
        description: "[SailfishMethod] runs a method as a sequential microbenchmark; [Trawl] runs it as a concurrent load scenario. The two execution modes are mutually exclusive.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            var attributeNames = method
                .AttributeLists
                .SelectMany(list => list.Attributes)
                .Select(attribute => attribute.Name.ToString())
                .ToList();

            var hasSailfishMethod = attributeNames.Any(name => SailfishMethodNames.Contains(name));
            var hasTrawl = attributeNames.Any(name => TrawlNames.Contains(name));

            if (hasSailfishMethod && hasTrawl)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, method.Identifier.GetLocation(), method.Identifier.Text));
        }
    }
}
