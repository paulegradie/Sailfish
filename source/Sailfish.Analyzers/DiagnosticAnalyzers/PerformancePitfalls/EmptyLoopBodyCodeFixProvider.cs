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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyLoopBodyCodeFixProvider))]
[Shared]
public sealed class EmptyLoopBodyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(EmptyLoopBodyAnalyzer.Descriptor.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        var loop = node.FirstAncestorOrSelf<SyntaxNode>(n => n is ForStatementSyntax || n is ForEachStatementSyntax || n is WhileStatementSyntax || n is DoStatementSyntax);
        if (loop is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add observable work (Consume) to loop body",
                c => AddConsumeToLoopAsync(context.Document, loop, c),
                nameof(EmptyLoopBodyCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> AddConsumeToLoopAsync(Document document, SyntaxNode loop, CancellationToken ct)
    {
        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        if (editor.OriginalRoot is CompilationUnitSyntax root)
        {
            if (!root.Usings.Any(u => u.Name?.ToString() == "Sailfish.Utilities"))
            {
                editor.ReplaceNode(root, root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Sailfish.Utilities"))));
            }
        }

        // Build statement: Consumer.Consume(0);
        var consumeStmt = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Consumer"),
                    SyntaxFactory.IdentifierName("Consume")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))))));

        switch (loop)
        {
            case ForStatementSyntax forStmt:
                editor.ReplaceNode(forStmt, forStmt.WithStatement(MakeBlock(forStmt.Statement, consumeStmt)));
                break;
            case ForEachStatementSyntax forEachStmt:
                editor.ReplaceNode(forEachStmt, forEachStmt.WithStatement(MakeBlock(forEachStmt.Statement, consumeStmt)));
                break;
            case WhileStatementSyntax whileStmt:
                editor.ReplaceNode(whileStmt, whileStmt.WithStatement(MakeBlock(whileStmt.Statement, consumeStmt)));
                break;
            case DoStatementSyntax doStmt:
                editor.ReplaceNode(doStmt, doStmt.WithStatement(MakeBlock(doStmt.Statement, consumeStmt)));
                break;
        }

        return editor.GetChangedDocument();
    }

    private static BlockSyntax MakeBlock(StatementSyntax? originalBody, StatementSyntax consumeStmt)
    {
        if (originalBody is BlockSyntax block)
        {
            // If it's an empty block, add consume; else append
            return block.Statements.Count == 0 ? block.AddStatements(consumeStmt) : block.AddStatements(consumeStmt);
        }

        // Replace empty statement or single statement with a new block containing the consume call and the original if not empty
        if (originalBody is EmptyStatementSyntax || originalBody is null)
        {
            return SyntaxFactory.Block(consumeStmt);
        }

        return SyntaxFactory.Block(originalBody, consumeStmt);
    }
}

