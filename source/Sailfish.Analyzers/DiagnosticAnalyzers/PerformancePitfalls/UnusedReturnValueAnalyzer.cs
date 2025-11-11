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
public sealed class UnusedReturnValueAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1001",
        title: "Unused return value inside SailfishMethod",
        messageFormat: "The return value of '{0}' is ignored; assign/use it or consume via a blackhole to avoid dead-code elimination",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        description: "Within [SailfishMethod] bodies, non-void method calls whose return values are ignored may be optimized away (DCE).",
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
            // Only analyze block-bodied methods; expression-bodied methods implicitly return their expression
            var body = method.Body;
            if (body is null) continue;

            foreach (var exprStmt in body.DescendantNodes().OfType<ExpressionStatementSyntax>())
            {
                // Handle awaited expressions: if awaiting a Task<T>/ValueTask<T>, the resulting T is produced and then ignored
                if (exprStmt.Expression is AwaitExpressionSyntax awaitExpr)
                {
                    var awaitedExpr = awaitExpr.Expression;
                    var awaitedType = semanticModel.GetTypeInfo(awaitExpr).Type; // type after await (i.e., T for Task<T>)

                    // If the awaited result type is not void, then a value is produced and discarded in this statement
                    if (awaitedType is not null && awaitedType.SpecialType != SpecialType.System_Void)
                    {
                        IMethodSymbol? awaitedMethod = null;
                        if (awaitedExpr is InvocationExpressionSyntax awaitedInvocation)
                        {
                            var awaitedSymInfo = semanticModel.GetSymbolInfo(awaitedInvocation);
                            awaitedMethod = awaitedSymInfo.Symbol as IMethodSymbol;
                        }

                        var diag = Diagnostic.Create(Descriptor, awaitExpr.GetLocation(), awaitedMethod?.Name ?? "awaited expression");
                        context.ReportDiagnostic(diag);
                        continue;
                    }

                    // Otherwise it's an await of Task/ValueTask (no result); nothing to report
                    continue;
                }

                if (exprStmt.Expression is InvocationExpressionSyntax invocation)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                    {
                        if (!methodSymbol.ReturnsVoid)
                        {
                            // Return value is ignored
                            var diagnostic = Diagnostic.Create(Descriptor, invocation.GetLocation(), methodSymbol.Name);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}

