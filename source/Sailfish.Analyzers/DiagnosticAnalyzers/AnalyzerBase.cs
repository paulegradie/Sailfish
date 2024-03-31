using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using System.Diagnostics;

namespace Sailfish.Analyzers.DiagnosticAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public abstract class AnalyzerBase<TSyntax> : DiagnosticAnalyzer where TSyntax : TypeDeclarationSyntax
{
    public override void Initialize(AnalysisContext analysisContext)
    {
        try
        {
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            if (!Debugger.IsAttached) analysisContext.EnableConcurrentExecution();
            analysisContext.RegisterSyntaxNodeAction(
                analyzeContext =>
                    AnalyzeNode((TSyntax)analyzeContext.Node,
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

    protected abstract void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context);
}