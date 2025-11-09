using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PerformancePitfalls;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstantOnlyComputationCodeFixProvider))]
[Shared]
public sealed class ConstantOnlyComputationCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ConstantOnlyComputationAnalyzer.Descriptor.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var constantExpr = node.FirstAncestorOrSelf<ExpressionSyntax>();
        var statement = constantExpr?.FirstAncestorOrSelf<StatementSyntax>();
        if (constantExpr is null || statement is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Consume constant result (reduce DCE risk)",
                c => InsertConsumeAfterAsync(context.Document, statement, constantExpr, c),
                nameof(ConstantOnlyComputationCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> InsertConsumeAfterAsync(Document document, StatementSyntax statement, ExpressionSyntax expr, CancellationToken ct)
    {
        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        if (editor.OriginalRoot is CompilationUnitSyntax root)
        {
            if (!root.Usings.Any(u => u.Name?.ToString() == "Sailfish.Utilities"))
            {
                editor.ReplaceNode(root, root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Sailfish.Utilities"))));
            }
        }

        // Build: Consumer.Consume((<expr>));
        var consumeInvocation = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Consumer"), SyntaxFactory.IdentifierName("Consume")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(Parenthesize(expr))))));

        editor.InsertAfter(statement, consumeInvocation);
        return editor.GetChangedDocument();
    }

    private static ExpressionSyntax Parenthesize(ExpressionSyntax expr)
    {
        return expr is ParenthesizedExpressionSyntax ? expr : SyntaxFactory.ParenthesizedExpression(expr);
    }
}

