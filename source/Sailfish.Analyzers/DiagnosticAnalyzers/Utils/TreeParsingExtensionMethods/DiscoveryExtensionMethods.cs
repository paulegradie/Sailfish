using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.Utils.TreeParsingExtensionMethods;

public static class DiscoveryExtensionMethods
{
    public static List<TypeDeclarationSyntax> FindAllTypesInInheritenceTree(this ISymbol typeSymbol, SemanticModel semanticModel)
    {
        var allTypesThatInheritFromThisType = semanticModel.Compilation
            .GetSymbolsWithName(n => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.BaseType is not null)
            .Where(t => t.BaseType?.Name == typeSymbol.Name)
            .SelectMany(t => t.DeclaringSyntaxReferences)
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>();
        var types = new List<TypeDeclarationSyntax>();
        foreach (var derivedType in allTypesThatInheritFromThisType)
        {
            types.Add(derivedType);
            types.AddRange(FindAllTypesInInheritenceTree(derivedType, semanticModel));
        }

        return types;
    }

    public static List<TypeDeclarationSyntax> FindAllTypesInInheritenceTree(this TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
    {
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);
        return typeSymbol is null ? new List<TypeDeclarationSyntax>() : FindAllTypesInInheritenceTree(typeSymbol, semanticModel);
    }

    public static bool HasSailfishAttribute(this TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
    {
        const string sailfishAttribute = "SailfishAttribute";
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

        if (typeSymbol == null)
            return false;

        if (typeSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == sailfishAttribute))
            return true;

        var compilation = semanticModel.Compilation;
        var allTypes = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>());
        var derivedTypes = allTypes
            .Where(t => t.BaseList?.Types.Any(bt => bt.Type.ToString() == typeDeclaration.Identifier.Text) ?? false)
            .ToList();

        foreach (var derivedType in derivedTypes)
        {
            var derivedSemanticModel = compilation.GetSemanticModel(derivedType.SyntaxTree);
            var derivedTypeSymbol = derivedSemanticModel.GetDeclaredSymbol(derivedType);

            if (derivedTypeSymbol != null && derivedTypeSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == sailfishAttribute))
                return true;
        }

        return false;
    }
}