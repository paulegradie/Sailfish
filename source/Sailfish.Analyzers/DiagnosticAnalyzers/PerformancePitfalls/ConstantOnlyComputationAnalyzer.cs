using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using System.Linq;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstantOnlyComputationAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1002",
        title: "Constant-only computation in SailfishMethod",
        messageFormat: "Benchmark method '{0}' performs computations using only constants/literals; this may be optimized away",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        description: "Within [SailfishMethod], computations using only constants/literals and no external inputs may be constant-folded or eliminated.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var sailfishMethods = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.HasAttributeAmong(new[] { "SailfishMethod" }));

        foreach (var method in sailfishMethods)
        {
            if (method.Body is null && method.ExpressionBody is null) continue;

            // Heuristic: if the method references any parameter/field/property identifiers, treat as using external input
            var identifiers = method.DescendantNodes().OfType<IdentifierNameSyntax>();
            var usesExternalInputs = identifiers
                .Select(id => semanticModel.GetSymbolInfo(id).Symbol)
                .Any(sym => sym is IParameterSymbol || sym is IFieldSymbol || sym is IPropertySymbol);

            if (usesExternalInputs) continue;

            // Look for arithmetic expressions where both operands are constants (literal or const symbol)
            var binaryExprs = method.DescendantNodes().OfType<BinaryExpressionSyntax>()
                .Where(b => b.IsKind(SyntaxKind.AddExpression)
                            || b.IsKind(SyntaxKind.SubtractExpression)
                            || b.IsKind(SyntaxKind.MultiplyExpression)
                            || b.IsKind(SyntaxKind.DivideExpression)
                            || b.IsKind(SyntaxKind.ModuloExpression));

            foreach (var bin in binaryExprs)
            {
                if (IsConstant(bin.Left, semanticModel) && IsConstant(bin.Right, semanticModel))
                {
                    var diagnostic = Diagnostic.Create(Descriptor, bin.GetLocation(), method.Identifier.Text);
                    context.ReportDiagnostic(diagnostic);
                    break; // One instance is enough per method
                }
            }
        }
    }

    private static bool IsConstant(ExpressionSyntax expr, SemanticModel semanticModel)
    {
        if (expr is LiteralExpressionSyntax) return true;
        if (expr is ParenthesizedExpressionSyntax p) return IsConstant(p.Expression, semanticModel);

        if (expr is IdentifierNameSyntax id)
        {
            var sym = semanticModel.GetSymbolInfo(id).Symbol;
            return sym is ILocalSymbol { IsConst: true } || sym is IFieldSymbol { IsConst: true };
        }

        // default conservative: not considered constant
        return false;
    }
}

