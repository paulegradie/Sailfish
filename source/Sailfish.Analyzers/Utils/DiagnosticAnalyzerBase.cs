using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics;

namespace Sailfish.Analyzers.Utils;

public abstract class DiagnosticAnalyzerBase : DiagnosticAnalyzer
{
    protected abstract (Action<SyntaxNodeAnalysisContext>, SyntaxKind[]) CreateAnalyzer(AnalysisContext context);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        if (!Debugger.IsAttached) context.EnableConcurrentExecution();

        (var analyzer, var syntaxKinds) = CreateAnalyzer(context);
        context.RegisterSyntaxNodeAction(analyzer, syntaxKinds);
    }
}