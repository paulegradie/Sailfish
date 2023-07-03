using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

public static class GeneralExtensionMethods
{
    public static bool IsClassPropertyOrField(this IdentifierNameSyntax identifierName)
    {
        // Get the immediate parent node
        var parent = identifierName.Parent;

        // Check if the parent is an ExpressionStatementSyntax
        if (parent is AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax })
            // Check if the parent's parent is a PropertyDeclarationSyntax
            return true;

        // Check if the parent is a VariableDeclaratorSyntax
        // Check if the parent's parent is a FieldDeclarationSyntax
        // Not a class property or field
        return parent is VariableDeclaratorSyntax { Parent: FieldDeclarationSyntax };
    }
}