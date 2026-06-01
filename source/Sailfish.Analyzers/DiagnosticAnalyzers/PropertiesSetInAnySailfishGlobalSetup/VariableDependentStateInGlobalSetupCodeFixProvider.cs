using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;

/// <summary>
///     Moves variable-dependent state out of <c>[SailfishGlobalSetup]</c> and into <c>[SailfishMethodSetup]</c>, the
///     hook that runs once per variable set. When GlobalSetup does nothing but build the variable-dependent state, the
///     attribute is simply re-applied; otherwise only the offending assignment is moved and the rest of GlobalSetup is
///     left untouched.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VariableDependentStateInGlobalSetupCodeFixProvider))]
[Shared]
public sealed class VariableDependentStateInGlobalSetupCodeFixProvider : CodeFixProvider
{
    private const string MethodSetupAttributeName = "SailfishMethodSetup";
    private const string GlobalSetupAttributeName = "SailfishGlobalSetup";
    private const string AttributeSuffix = "Attribute";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(VariableDependentStateInGlobalSetupAnalyzer.Descriptor.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var identifier = node.FirstAncestorOrSelf<IdentifierNameSyntax>();

        // We can only move the variable read if it lives inside an assignment. A bare read (e.g. logging the variable)
        // is still flagged by the analyzer, but there is no statement we can safely relocate.
        var assignment = identifier?.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
        if (assignment is null) return;

        var hookMethod = assignment.FirstAncestorOrSelf<MethodDeclarationSyntax>();

        // Only offer the fix for GlobalSetup. A variable read inside GlobalTeardown is still diagnosed, but relocating
        // teardown logic into MethodSetup is not a meaningful transform.
        if (hookMethod is null || !HasAttribute(hookMethod, GlobalSetupAttributeName)) return;
        if (hookMethod.Parent is not TypeDeclarationSyntax) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Move variable-dependent setup to [SailfishMethodSetup]",
                c => MoveToMethodSetupAsync(context.Document, hookMethod, assignment, c),
                nameof(VariableDependentStateInGlobalSetupCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> MoveToMethodSetupAsync(
        Document document,
        MethodDeclarationSyntax globalSetup,
        AssignmentExpressionSyntax assignment,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || globalSetup.Parent is not TypeDeclarationSyntax classDeclaration) return document;

        // The top-level statement directly under the GlobalSetup block that contains the assignment. The assignment
        // may be nested (inside an if/for/while), so we relocate the whole containing top-level statement — removing
        // the nested node from Body.Statements would be a no-op and would duplicate it into MethodSetup.
        var topLevelStatement = globalSetup.Body is null
            ? null
            : assignment.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault(s => s.Parent == globalSetup.Body);

        // Sole-purpose GlobalSetup: its entire body is this assignment, so the method should simply have been a
        // MethodSetup. Re-attribute it in place — minimal change, preserves the body and its formatting verbatim.
        var isSolePurpose =
            (globalSetup.ExpressionBody is not null && globalSetup.ExpressionBody.Expression == assignment) ||
            (globalSetup.Body is { } soleBody && soleBody.Statements.Count == 1 && soleBody.Statements[0] == topLevelStatement);

        if (isSolePurpose)
        {
            var reAttributed = RenameAttribute(globalSetup, GlobalSetupAttributeName, MethodSetupAttributeName);
            return document.WithSyntaxRoot(root.ReplaceNode(globalSetup, reAttributed));
        }

        // Mixed body: split. Move the whole top-level statement; leave every unrelated GlobalSetup statement in place.
        if (topLevelStatement is null || globalSetup.Body is null) return document;

        var movedStatement = topLevelStatement.WithoutTrivia()
            .WithAdditionalAnnotations(Formatter.Annotation);

        var trimmedGlobalSetup = globalSetup.WithBody(
            globalSetup.Body.WithStatements(globalSetup.Body.Statements.Remove(topLevelStatement)));

        var existingMethodSetup = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => HasAttribute(m, MethodSetupAttributeName));

        // Work by index against the original member list. Reference equality against a post-Replace list is unreliable
        // because Replace re-wraps the surrounding red nodes, so IndexOf on the new list would not find them.
        var members = classDeclaration.Members.ToList();
        var globalSetupIndex = members.IndexOf(globalSetup);
        members[globalSetupIndex] = trimmedGlobalSetup;

        if (existingMethodSetup is not null)
            members[members.IndexOf(existingMethodSetup)] = AppendStatement(existingMethodSetup, movedStatement);
        else
            members.Insert(globalSetupIndex + 1, CreateMethodSetup(movedStatement));

        return document.WithSyntaxRoot(
            root.ReplaceNode(classDeclaration, classDeclaration.WithMembers(SyntaxFactory.List(members))));
    }

    private static MethodDeclarationSyntax RenameAttribute(MethodDeclarationSyntax method, string fromName, string toName)
    {
        var attribute = method.AttributeLists
            .SelectMany(list => list.Attributes)
            .First(a => SimpleAttributeName(a.Name) == fromName);

        var renamed = attribute.WithName(SyntaxFactory.IdentifierName(toName).WithTriviaFrom(attribute.Name));
        return method.ReplaceNode(attribute, renamed);
    }

    private static MethodDeclarationSyntax AppendStatement(MethodDeclarationSyntax method, StatementSyntax statement)
    {
        if (method.Body is not null)
            return method.WithBody(method.Body.AddStatements(statement));

        if (method.ExpressionBody is not null)
            return method
                .WithExpressionBody(null)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(method.ExpressionBody.Expression), statement))
                .WithAdditionalAnnotations(Formatter.Annotation);

        return method.WithBody(SyntaxFactory.Block(statement)).WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static MethodDeclarationSyntax CreateMethodSetup(StatementSyntax statement)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "MethodSetup")
            .WithAttributeLists(SyntaxFactory.SingletonList(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(MethodSetupAttributeName))))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBody(SyntaxFactory.Block(statement))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static bool HasAttribute(MethodDeclarationSyntax method, string attributeName)
    {
        return method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(a => SimpleAttributeName(a.Name) == attributeName);
    }

    private static string SimpleAttributeName(NameSyntax name)
    {
        var text = name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            SimpleNameSyntax simple => simple.Identifier.Text,
            _ => name.ToString()
        };

        return text.EndsWith(AttributeSuffix) ? text.Substring(0, text.Length - AttributeSuffix.Length) : text;
    }
}
