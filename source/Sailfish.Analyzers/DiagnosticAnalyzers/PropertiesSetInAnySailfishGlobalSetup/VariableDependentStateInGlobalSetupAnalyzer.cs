using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Sailfish.Analyzers.Utils;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.PropertiesSetInAnySailfishGlobalSetup;

/// <summary>
///     Flags reads of a Sailfish variable member (<c>[SailfishVariable]</c>, <c>[SailfishRangeVariable]</c>, or an
///     <c>ISailfishVariables&lt;,&gt;</c> property) inside a once-per-class lifecycle hook
///     (<c>[SailfishGlobalSetup]</c> / <c>[SailfishGlobalTeardown]</c>).
///     <para>
///         GlobalSetup runs exactly once for the whole class (at the first method's first variable set). The instance
///         state it produces is captured by the engine and replayed onto every subsequent test-case instance, while the
///         variable property itself is re-injected per case. So any field built from a variable inside GlobalSetup is
///         silently frozen at a single value for every test case — the benchmark measures one input size for all cases
///         and ScaleFish reports ~O(1). Variable-dependent state belongs in <c>[SailfishMethodSetup]</c>, which runs per
///         variable set after the replay.
///     </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VariableDependentStateInGlobalSetupAnalyzer : AnalyzerBase<ClassDeclarationSyntax>
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        id: "SF1016",
        title: "Sailfish variable read inside a once-per-class lifecycle hook",
        messageFormat:
        "'{0}' is a Sailfish variable read inside [{1}], which runs once per class. State derived from '{0}' there is frozen at a single value while '{0}' advances for every test case, so the benchmark silently measures one value of '{0}'. Build variable-dependent state in [SailfishMethodSetup] instead.",
        category: AnalyzerGroups.EssentialAnalyzers.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: AnalyzerGroups.EssentialAnalyzers.IsEnabledByDefault,
        description:
        "Reading a [SailfishVariable] (or [SailfishRangeVariable], or an ISailfishVariables<,> property) inside [SailfishGlobalSetup]/[SailfishGlobalTeardown] is almost always a bug. These hooks run once per class, and any field state derived from the variable is captured and replayed across every variable set, so the variable is silently frozen at a single value. Build variable-dependent state in [SailfishMethodSetup], which runs once per variable set.",
        helpLinkUri: AnalyzerGroups.EssentialAnalyzers.HelpLink);

    /// <summary>
    ///     The once-per-class lifecycle hooks. Reads of a variable here cannot vary across test cases.
    ///     Per-variable-set hooks (MethodSetup/IterationSetup) and the benchmark body are intentionally excluded.
    /// </summary>
    private static readonly string[] OncePerClassHookAttributeNames =
    [
        "SailfishGlobalSetup",
        "SailfishGlobalTeardown"
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    protected override void AnalyzeNode(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
    {
        if (!classDeclaration.IsASailfishTestType(semanticModel)) return;

        var hookMethods = classDeclaration
            .Members
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.HasAttributeAmong(OncePerClassHookAttributeNames))
            .ToList();

        // One diagnostic per (statement-or-hook, variable) so that `N + N` or repeated reads don't double-report.
        var reported = new HashSet<(SyntaxNode Unit, string Variable)>();

        foreach (var hookMethod in hookMethods)
        {
            var hookName = hookMethod.GetAllAttributesAmong(OncePerClassHookAttributeNames).First();

            foreach (var identifier in hookMethod.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                // nameof(N) is a compile-time constant string, not a read of the variable's value.
                if (IsInsideNameof(identifier)) continue;

                // `N = ...` is a write to the variable, not a read of its (frozen) value.
                if (IsSimpleAssignmentTarget(identifier)) continue;

                if (semanticModel.GetSymbolInfo(identifier).Symbol is not IPropertySymbol propertySymbol) continue;
                if (!IsSailfishVariableMember(propertySymbol)) continue;

                var unit = (SyntaxNode?)identifier.FirstAncestorOrSelf<StatementSyntax>() ?? hookMethod;
                if (!reported.Add((unit, propertySymbol.Name))) continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptor,
                    identifier.GetLocation(),
                    propertySymbol.Name,
                    hookName));
            }
        }
    }

    private static bool IsSailfishVariableMember(IPropertySymbol property)
    {
        // Attribute-based variables: [SailfishVariable] / [SailfishRangeVariable].
        // Matched by attribute class name to stay consistent with the rest of the analyzers and to work even when the
        // attribute type is only available through a reference (no need to bind to ISailfishVariableAttribute).
        if (property.GetAttributes().Any(a =>
                a.AttributeClass is { } attributeClass &&
                attributeClass.Name is "SailfishVariableAttribute" or "SailfishRangeVariableAttribute"))
            return true;

        // Interface/class-based variables: a property whose type is (or implements) ISailfishVariables<,>.
        var propertyType = property.Type;
        return propertyType.Name == "ISailfishVariables" ||
               propertyType.AllInterfaces.Any(i => i.Name == "ISailfishVariables");
    }

    private static bool IsSimpleAssignmentTarget(IdentifierNameSyntax identifier)
    {
        return identifier.Parent is AssignmentExpressionSyntax assignment &&
               assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
               assignment.Left == identifier;
    }

    private static bool IsInsideNameof(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } })
                return true;

            // nameof arguments are syntactically shallow; stop at the first statement/member boundary.
            if (current is StatementSyntax or MemberDeclarationSyntax) break;
        }

        return false;
    }
}
