using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyLoopBodyAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1003",
        title: "Empty loop body inside SailfishMethod",
        messageFormat: "Loop has an empty body; it may be optimized away",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        description: "Empty loop bodies in benchmarks can be eliminated by the compiler/JIT and do not measure meaningful work.",
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
            var body = method.Body;
            if (body is null) continue;

            foreach (var loop in body.DescendantNodes().OfType<StatementSyntax>())
            {
                switch (loop)
                {
                    case ForStatementSyntax forStmt:
                        if (IsEmpty(forStmt.Statement))
                        {
                            var diagnostic = Diagnostic.Create(Descriptor, forStmt.ForKeyword.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                        break;
                    case ForEachStatementSyntax forEachStmt:
                        if (IsEmpty(forEachStmt.Statement))
                        {
                            var diagnostic = Diagnostic.Create(Descriptor, forEachStmt.ForEachKeyword.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                        break;
                    case ForEachVariableStatementSyntax forEachVarStmt:
                        if (IsEmpty(forEachVarStmt.Statement))
                        {
                            var diagnostic = Diagnostic.Create(Descriptor, forEachVarStmt.ForEachKeyword.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                        break;

                    case WhileStatementSyntax whileStmt:
                        if (IsEmpty(whileStmt.Statement))
                        {
                            var diagnostic = Diagnostic.Create(Descriptor, whileStmt.WhileKeyword.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                        break;
                    case DoStatementSyntax doStmt:
                        if (IsEmpty(doStmt.Statement))
                        {
                            var diagnostic = Diagnostic.Create(Descriptor, doStmt.DoKeyword.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                        break;
                }
            }
        }
    }

    private static bool IsEmpty(StatementSyntax stmt)
    {
        if (stmt is EmptyStatementSyntax) return true; // e.g., for(...);
        if (stmt is BlockSyntax block)
        {
            if (block.Statements.Count == 0) return true;
            return block.Statements.All(s => s is EmptyStatementSyntax);
        }
        return false;
    }
}

