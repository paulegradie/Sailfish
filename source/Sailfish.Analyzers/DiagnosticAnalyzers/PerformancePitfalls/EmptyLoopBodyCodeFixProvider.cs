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

        var loop = node.FirstAncestorOrSelf<SyntaxNode>(n => n is ForStatementSyntax || n is ForEachStatementSyntax || n is ForEachVariableStatementSyntax || n is WhileStatementSyntax || n is DoStatementSyntax);
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
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false) as CompilationUnitSyntax;
        if (root is null) return document;

        // Build statement: Consumer.Consume(0);
        var consumeStmt = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Consumer"),
                    SyntaxFactory.IdentifierName("Consume")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(0)))))));

        var newLoop = loop switch
        {
            ForStatementSyntax forStmt => forStmt.WithStatement(MakeBlock(forStmt.Statement, consumeStmt)),
            ForEachStatementSyntax forEachStmt => forEachStmt.WithStatement(MakeBlock(forEachStmt.Statement, consumeStmt)),
            ForEachVariableStatementSyntax forEachVarStmt => forEachVarStmt.WithStatement(MakeBlock(forEachVarStmt.Statement, consumeStmt)),
            WhileStatementSyntax whileStmt => whileStmt.WithStatement(MakeBlock(whileStmt.Statement, consumeStmt)),
            DoStatementSyntax doStmt => doStmt.WithStatement(MakeBlock(doStmt.Statement, consumeStmt)),
            _ => loop
        };

        var newRoot = (CompilationUnitSyntax)root.ReplaceNode(loop, newLoop);

        // Ensure using Sailfish.Utilities exists
        if (!newRoot.Usings.Any(u => u.Name?.ToString() == "Sailfish.Utilities"))
        {
            var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Sailfish.Utilities"));
            // Preserve trivia from the last using directive
            if (newRoot.Usings.Count > 0)
            {
                var lastUsing = newRoot.Usings[newRoot.Usings.Count - 1];
                newUsing = newUsing.WithTrailingTrivia(lastUsing.GetTrailingTrivia());
            }
            newRoot = newRoot.AddUsings(newUsing);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static BlockSyntax MakeBlock(StatementSyntax? originalBody, StatementSyntax consumeStmt)
    {
        if (originalBody is BlockSyntax block)
        {
            // Add consume statement to the block
            return block.AddStatements(consumeStmt);
        }

        // Replace empty statement or single statement with a new block containing the consume call and the original if not empty
        if (originalBody is EmptyStatementSyntax || originalBody is null)
        {
            return SyntaxFactory.Block(consumeStmt);
        }

        return SyntaxFactory.Block(originalBody, consumeStmt);
    }
}

