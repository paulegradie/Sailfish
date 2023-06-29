using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.Utils;

public abstract class DiagnosticAnalyzerBase : DiagnosticAnalyzer
{
    protected abstract (Action<SyntaxNodeAnalysisContext>, SyntaxKind[]) CreateAnalyzer(AnalysisContext context);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        if (!Debugger.IsAttached) context.EnableConcurrentExecution();

        var (analyzer, syntaxKinds) = CreateAnalyzer(context);
        context.RegisterSyntaxNodeAction(analyzer, syntaxKinds);
    }
}