using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

public static class IsASailfishTestTypeExtensionMethods
{
    public static bool IsASailfishTestType(this TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
    {
        const string sailfishAttribute = "SailfishAttribute";
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

        if (typeSymbol == null) return false;

        if (typeSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == sailfishAttribute)) return true;

        var compilation = semanticModel.Compilation;

        var allTypesInHierarchy = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()).ToList();
        var derivedTypes = allTypesInHierarchy
            .Where(t => t.BaseList?.Types.Any(bt => bt.Type.ToString() == typeDeclaration.Identifier.Text) ?? false)
            .ToList();

        foreach (var derivedType in derivedTypes)
        {
            var derivedSemanticModel = compilation.GetSemanticModel(derivedType.SyntaxTree);
            var derivedTypeSymbol = derivedSemanticModel.GetDeclaredSymbol(derivedType);

            if (derivedTypeSymbol != null && derivedTypeSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == sailfishAttribute)) return true;
        }

        return false;
    }
}