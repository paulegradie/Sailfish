using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.Trawl;

/// <summary>
///     SF1023 — flags an unsynchronized write to mutable instance state from inside a <c>[Trawl]</c> load
///     scenario. A <c>[Trawl]</c> method is invoked <i>concurrently</i> by many virtual users against a single
///     shared test instance (the engine builds one delegate from <c>container.Instance</c> and every worker
///     invokes it), so writing an instance field or auto-property per request is a data race.
///     <para>
///         Detection is deliberately scoped to the obvious footgun: a direct write (assignment, compound
///         assignment, or <c>++</c>/<c>--</c>) to a non-<c>readonly</c> instance field or auto-property on the
///         current instance (a bare name, <c>this.x</c>, or <c>base.x</c>), in the <c>[Trawl]</c> method body
///         itself. Writes lexically inside a <c>lock</c> are skipped, and <c>Interlocked.*</c> calls are not
///         writes in this sense (the field is passed by <c>ref</c>), so neither is flagged. Locals, parameters,
///         static state, and writes through any other receiver are left alone. Because precise
///         per-request-mutation detection requires dataflow and will have false positives (e.g. a field written
///         once and then only read), this is a <see cref="DiagnosticSeverity.Warning" /> — a strong smell, not a
///         guaranteed bug. Writes reached only through a helper method are intentionally out of scope for the MVP.
///     </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TrawlSharedInstanceMutationAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    private const string TrawlAttributeName = "TrawlAttribute";

    public static readonly DiagnosticDescriptor Descriptor = new(
        "SF1023",
        "Shared instance state mutated in a [Trawl] load scenario",
        "'{0}' is mutable instance state written inside [Trawl] method '{1}'; all virtual users share one test instance, so this is an unsynchronized data race. Use a local, make the scenario stateless, or guard the write with a lock or Interlocked.",
        AnalyzerGroups.EssentialAnalyzers.Category,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        defaultSeverity: DiagnosticSeverity.Warning,
        description:
        "A [Trawl] load scenario is invoked concurrently by many virtual users against a single shared test instance, so any unsynchronized write to instance state is a data race. Keep scenario state in locals, make the scenario stateless, or synchronize the write (lock/Interlocked).",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            // Resolve [Trawl] semantically (via the bound symbol) rather than by syntactic name, mirroring SF1022, so
            // namespace-qualified, global::-qualified, or aliased usages are still detected.
            if (semanticModel.GetDeclaredSymbol(method) is not { } methodSymbol) continue;
            if (!methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == TrawlAttributeName)) continue;

            AnalyzeTrawlMethod(method, semanticModel, context);
        }
    }

    private static void AnalyzeTrawlMethod(MethodDeclarationSyntax method, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        foreach (var node in method.DescendantNodes())
        {
            // The target of a write: the left of any assignment (=, +=, ??=, <<=, ...) or the operand of ++/--.
            var target = node switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left,
                PrefixUnaryExpressionSyntax prefix when IsIncrementOrDecrement(prefix.Kind()) => prefix.Operand,
                PostfixUnaryExpressionSyntax postfix when IsIncrementOrDecrement(postfix.Kind()) => postfix.Operand,
                _ => null
            };

            if (target is null) continue;

            // Best-effort: a write taken under a lock in this method body is synchronized. (Interlocked.* needs no
            // special handling — it passes the field by ref, so it is never an assignment/increment target.)
            if (IsInsideLock(target, method)) continue;

            if (TryGetMutatedInstanceMember(target, semanticModel) is not { } member) continue;

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, target.GetLocation(), member.Name, method.Identifier.Text));
        }
    }

    private static bool IsIncrementOrDecrement(SyntaxKind kind) =>
        kind is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression
            or SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression;

    /// <summary>
    ///     Returns the field or auto-property the target writes to, but only when the write is to mutable instance
    ///     state on the <i>current</i> instance (a bare name, <c>this.x</c>, or <c>base.x</c>). Returns null for
    ///     locals, parameters, static members, <c>readonly</c>/<c>const</c> fields, properties with a custom setter,
    ///     and writes through any other receiver (e.g. <c>other.field</c> or <c>this.obj.field</c>, which do not
    ///     reassign instance state of the shared test instance).
    /// </summary>
    private static ISymbol? TryGetMutatedInstanceMember(ExpressionSyntax target, SemanticModel semanticModel)
    {
        switch (target)
        {
            case IdentifierNameSyntax:
            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax }:
                break;
            default:
                return null;
        }

        return semanticModel.GetSymbolInfo(target).Symbol switch
        {
            IFieldSymbol { IsStatic: false, IsReadOnly: false, IsConst: false } field => field,
            IPropertySymbol { IsStatic: false, SetMethod: not null } property when IsAutoProperty(property) => property,
            _ => null
        };
    }

    /// <summary>
    ///     True only for an auto-implemented property (<c>{ get; set; }</c>). A property with a hand-written setter is
    ///     deliberately not flagged: the setter may synchronize internally, and chasing the field it writes is dataflow
    ///     we explicitly avoid for the MVP.
    /// </summary>
    private static bool IsAutoProperty(IPropertySymbol property)
    {
        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not PropertyDeclarationSyntax declaration) continue;
            if (declaration.ExpressionBody is not null) return false; // => computed, not an auto-property
            if (declaration.AccessorList is null) return false;

            return declaration.AccessorList.Accessors.All(a => a.Body is null && a.ExpressionBody is null);
        }

        return false;
    }

    private static bool IsInsideLock(SyntaxNode node, SyntaxNode boundary)
    {
        for (var current = node.Parent; current is not null && current != boundary; current = current.Parent)
            if (current is LockStatementSyntax)
                return true;

        return false;
    }
}
