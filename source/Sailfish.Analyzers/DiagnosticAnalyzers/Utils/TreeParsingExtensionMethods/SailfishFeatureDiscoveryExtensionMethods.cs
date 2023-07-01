using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sailfish.Analyzers.DiagnosticAnalyzers.Utils.TreeParsingExtensionMethods;

public static class SailfishFeatureDiscoveryExtensionMethods
{
    public static bool IsSailfishVariableProperty(this PropertyDeclarationSyntax propertyDeclaration, SyntaxNodeAnalysisContext context)
    {
        var variableAttribute = propertyDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attr => context.SemanticModel.GetTypeInfo(attr).Type?.Name == "SailfishVariableAttribute");
        return variableAttribute is not null;
    }
    
    
}