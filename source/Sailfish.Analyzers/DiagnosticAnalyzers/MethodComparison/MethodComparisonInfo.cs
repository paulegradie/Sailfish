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
    public string? ComparisonGroup { get; }
    public bool IsBaseline { get; }
    public AttributeArgumentSyntax? ComparisonGroupArgument { get; }
    public AttributeArgumentSyntax? IsBaselineArgument { get; }
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
}
