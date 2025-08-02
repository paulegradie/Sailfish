using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;

public static class SailfishFeatureDiscoveryExtensionMethods
{
    public static bool IsSailfishVariableProperty(this PropertyDeclarationSyntax propertyDeclaration)
    {
        return propertyDeclaration.HasAttributesWithNames("SailfishVariable");
    }

    public static bool IsSailfishVariableProperty(this IdentifierNameSyntax nameSyntax)
    {
        return nameSyntax.Identifier.Text == "SailfishVariable";
    }
}

public static class NodeExtensionMethods
{
    public static IEnumerable<TypeDeclarationSyntax> FindAllTypesInInheritanceTree(this TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
    {
        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);
        return typeSymbol is null ? new List<TypeDeclarationSyntax>() : FindAllTypesInInheritanceTree(typeSymbol, semanticModel);
    }

    public static List<TypeDeclarationSyntax> FindAllTypesInInheritanceTree(this ISymbol typeSymbol, SemanticModel semanticModel)
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
            types.AddRange(FindAllTypesInInheritanceTree(derivedType, semanticModel));
        }

        return types;
    }

    public static bool HasAttributesWithNames(this MethodDeclarationSyntax methodDeclarationSyntax, params string[] attributeName)
    {
        return methodDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes).All(a => attributeName.Contains(a.Name.ToString()));
    }

    public static bool HasAttributeAmong(this MethodDeclarationSyntax methodDeclarationSyntax, IEnumerable<string> attributeNames)
    {
        return methodDeclarationSyntax.GetAllAttributesAmong(attributeNames).Any();
    }

    public static IEnumerable<string> GetAllAttributesAmong(this MethodDeclarationSyntax methodDeclarationSyntax, IEnumerable<string> attributeNames)
    {
        return methodDeclarationSyntax.AttributeLists.SelectMany(a => a.Attributes).Select(s => s.Name.ToString()).Intersect(attributeNames);
    }

    public static bool HasAttributesWithNames(this PropertyDeclarationSyntax propertyDeclarationSyntax, params string[] attributeNames)
    {
        var foundAttributes = propertyDeclarationSyntax
            .AttributeLists
            .SelectMany(x => x.Attributes)
            .Select(x => x.Name.ToString())
            .ToList();
        if (!foundAttributes.Any()) return false;
        return attributeNames.Intersect(foundAttributes).Count() == attributeNames.Length;
    }
}