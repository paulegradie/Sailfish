using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.MethodComparison;

/// <summary>
/// Parsed view of the comparison-related arguments on a method's <c>[SailfishMethod]</c> attribute.
/// Returned by <see cref="MethodComparisonAttributeReader.TryRead"/>.
/// </summary>
internal sealed class MethodComparisonInfo
{
    public MethodComparisonInfo(
        MethodDeclarationSyntax method,
        AttributeSyntax attribute,
        string? comparisonGroup,
        bool isBaseline,
        AttributeArgumentSyntax? comparisonGroupArgument,
        AttributeArgumentSyntax? isBaselineArgument)
    {
        Method = method;
        Attribute = attribute;
        ComparisonGroup = comparisonGroup;
        IsBaseline = isBaseline;
        ComparisonGroupArgument = comparisonGroupArgument;
        IsBaselineArgument = isBaselineArgument;
    }

    public MethodDeclarationSyntax Method { get; }
    public AttributeSyntax Attribute { get; }

    /// <summary>The explicit <c>ComparisonGroup</c> value from <c>[SailfishMethod]</c>, or null if not set.</summary>
    public string? ComparisonGroup { get; }
    public bool IsBaseline { get; }
    public AttributeArgumentSyntax? ComparisonGroupArgument { get; }
    public AttributeArgumentSyntax? IsBaselineArgument { get; }

    /// <summary>
    /// Returns the effective comparison-group key for this method, given whether the enclosing
    /// class opts out of the implicit class-wide group via <c>[Sailfish(DisableComparison = true)]</c>.
    ///   <list type="bullet">
    ///     <item><description>Explicit <see cref="ComparisonGroup"/> wins regardless of class setting.</description></item>
    ///     <item><description>If no explicit group and class allows implicit: returns <see cref="ImplicitGroupKey"/>.</description></item>
    ///     <item><description>If no explicit group and class opts out: returns <c>null</c> (method not in any group).</description></item>
    ///   </list>
    /// </summary>
    public string? EffectiveGroupKey(bool classDisablesComparison)
    {
        if (!string.IsNullOrEmpty(ComparisonGroup)) return ComparisonGroup;
        return classDisablesComparison ? null : ImplicitGroupKey;
    }

    /// <summary>
    /// Sentinel used as the group key for methods that join the implicit class-wide comparison group.
    /// Distinct from any user-typed string (contains characters that aren't valid in a C# identifier).
    /// </summary>
    public const string ImplicitGroupKey = "<implicit class-wide>";
}

internal static class MethodComparisonAttributeReader
{
    public static MethodComparisonInfo? TryRead(MethodDeclarationSyntax method)
    {
        var sailfishMethodAttr = method.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attr =>
                attr.Name.ToString() == "SailfishMethod" ||
                attr.Name.ToString() == "SailfishMethodAttribute");

        if (sailfishMethodAttr is null) return null;

        string? comparisonGroup = null;
        var isBaseline = false;
        AttributeArgumentSyntax? comparisonGroupArg = null;
        AttributeArgumentSyntax? isBaselineArg = null;

        if (sailfishMethodAttr.ArgumentList?.Arguments != null)
        {
            foreach (var arg in sailfishMethodAttr.ArgumentList.Arguments)
            {
                var argName = arg.NameEquals?.Name.Identifier.ValueText;
                if (argName == "ComparisonGroup")
                {
                    comparisonGroupArg = arg;
                    if (arg.Expression is LiteralExpressionSyntax stringLit &&
                        // RS1034 wants IsKind on SyntaxToken, but that extension isn't available in this Roslyn target.
#pragma warning disable RS1034
                        stringLit.Token.Kind() == SyntaxKind.StringLiteralToken)
#pragma warning restore RS1034
                    {
                        comparisonGroup = stringLit.Token.ValueText;
                    }
                }
                else if (argName == "IsBaseline")
                {
                    isBaselineArg = arg;
                    if (arg.Expression is LiteralExpressionSyntax boolLit)
                    {
                        var kind = boolLit.Token.Kind();
                        if (kind == SyntaxKind.TrueKeyword) isBaseline = true;
                        else if (kind == SyntaxKind.FalseKeyword) isBaseline = false;
                    }
                }
            }
        }

        return new MethodComparisonInfo(method, sailfishMethodAttr, comparisonGroup, isBaseline, comparisonGroupArg, isBaselineArg);
    }

    /// <summary>
    /// Returns true when the class's <c>[Sailfish]</c> attribute sets <c>DisableComparison = true</c>
    /// as a boolean literal.
    /// </summary>
    public static bool ClassDisablesComparison(ClassDeclarationSyntax classDeclaration)
    {
        var sailfishAttr = classDeclaration.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .FirstOrDefault(attr =>
                attr.Name.ToString() == "Sailfish" ||
                attr.Name.ToString() == "SailfishAttribute");

        if (sailfishAttr?.ArgumentList?.Arguments == null) return false;

        foreach (var arg in sailfishAttr.ArgumentList.Arguments)
        {
            if (arg.NameEquals?.Name.Identifier.ValueText != "DisableComparison") continue;
            if (arg.Expression is LiteralExpressionSyntax lit)
            {
                var kind = lit.Token.Kind();
                if (kind == SyntaxKind.TrueKeyword) return true;
            }
        }
        return false;
    }
}
