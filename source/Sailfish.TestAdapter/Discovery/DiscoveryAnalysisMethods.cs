using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// ReSharper disable HeapView.ClosureAllocation

// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident

namespace Sailfish.TestAdapter.Discovery;

public static class DiscoveryAnalysisMethods
{
    /// <summary>
    /// </summary>
    /// <param name="sourceFiles"></param>
    /// <param name="performanceTestTypes"></param>
    /// <param name="classAttributePrefix"></param>
    /// <param name="methodAttributePrefix"></param>
    /// <returns>Dictionary of className:ClassMetaData</returns>
    /// <exception cref="Exception"></exception>
    public static IEnumerable<ClassMetaData> CompilePreRenderedSourceMap(
        IEnumerable<string> sourceFiles,
        Type[] performanceTestTypes,
        string classAttributePrefix = "Sailfish",
        string methodAttributePrefix = "SailfishMethod")
    {
        var classMetas = new ConcurrentBag<ClassMetaData>();
        var result = Parallel.ForEach(
            sourceFiles,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            filePath =>
            {
                string fileContents;
                try
                {
                    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var streamReader = new StreamReader(fileStream);
                    fileContents = streamReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    // Use Debug.WriteLine instead of Console.WriteLine to avoid RS1035 analyzer warning
                    // Console operations are banned in analyzers as they can block threads during parallel execution
                    Debug.WriteLine($"[Sailfish.TestAdapter] Failed to read file '{filePath}': {ex.Message}");
                    return;
                }

                // ReSharper disable once HeapView.ClosureAllocation
                SyntaxTree syntaxTree;
                try
                {
                    syntaxTree = CSharpSyntaxTree.ParseText(fileContents);
                }
                catch (Exception ex)
                {
                    // Use Debug.WriteLine instead of Console.WriteLine to avoid RS1035 analyzer warning
                    // Console operations are banned in analyzers as they can block threads during parallel execution
                    Debug.WriteLine($"[Sailfish.TestAdapter] Failed to parse syntax tree for file '{filePath}': {ex.Message}");
                    return;
                }

                var root = syntaxTree.GetCompilationUnitRoot();

                var classDeclarations = root
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(cls => cls
                        .AttributeLists
                        .SelectMany(attrList => attrList.Attributes)
                        .Any(attr =>
                            attr.Name.ToString() == classAttributePrefix || attr.Name.ToString() == $"{classAttributePrefix}Attribute"));

                // ReSharper disable once HeapView.ObjectAllocation.Possible
                foreach (var classDeclaration in classDeclarations)
                {
                    var methodDeclarations = classDeclaration
                        .DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .Where(method => method.AttributeLists
                            .SelectMany(attrList => attrList.Attributes)
                            .Any(attr =>
                                attr.Name.ToString() == methodAttributePrefix
                                || attr.Name.ToString() == $"{methodAttributePrefix}Attribute"));

                    // var className = classDeclaration.Identifier.ValueText;
                    var classFullName = RetrieveClassFullName(classDeclaration);
                    var performanceTestType = performanceTestTypes.SingleOrDefault(x => x.FullName == classFullName);
                    if (performanceTestType is null) continue;

                    var classMetaData = new ClassMetaData(
                        classFullName: classFullName,
                        performanceTestType: performanceTestType,
                        filePath: filePath,
                        methods: (
                            from methodDeclaration in methodDeclarations
                            let lineSpan = syntaxTree.GetLineSpan(methodDeclaration.Span)
                            let lineNumber = lineSpan.StartLinePosition.Line + 1
                            let comparisonGroup = ExtractComparisonInfo(methodDeclaration)
                            select new MethodMetaData(
                                methodDeclaration.Identifier.ValueText,
                                lineNumber,
                                comparisonGroup))
                        .ToArray(),
                        syntaxTree: syntaxTree);

                    classMetas.Add(classMetaData);
                }
            });

        if (!result.IsCompleted) throw new TestAdapterException("Exception encountered while reading and parsing source files");
        return classMetas;
    }

    private static string RetrieveClassFullName(ClassDeclarationSyntax classDeclarationSyntax)
    {
        var parentNode = classDeclarationSyntax.Parent;
        var fullClassName = "";
        while (parentNode != null)
        {
            if (parentNode is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                fullClassName = $"{fileScopedNamespace.Name}.{classDeclarationSyntax.Identifier.ValueText}";
                break;
            }
            else if (parentNode is NamespaceDeclarationSyntax regularNamespace)
            {
                fullClassName = $"{regularNamespace.Name}.{classDeclarationSyntax.Identifier.ValueText}";
                break;
            }

            parentNode = parentNode.Parent;
        }

        return fullClassName;
    }

    /// <summary>
    /// Extracts comparison group information from a method declaration's attributes.
    /// </summary>
    /// <param name="methodDeclaration">The method declaration to analyze.</param>
    /// <returns>The comparison group name, or null if no comparison attribute is found.</returns>
    private static string? ExtractComparisonInfo(MethodDeclarationSyntax methodDeclaration)
    {
        // Look for SailfishComparison attribute
        var comparisonAttribute = methodDeclaration.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .FirstOrDefault(attr =>
                attr.Name.ToString() == "SailfishComparison" ||
                attr.Name.ToString() == "SailfishComparisonAttribute");

        if (comparisonAttribute?.ArgumentList?.Arguments == null || comparisonAttribute.ArgumentList.Arguments.Count < 1)
        {
            return null;
        }

        try
        {
            // Extract comparison group (first and only argument)
            var groupArgument = comparisonAttribute.ArgumentList.Arguments[0];
            return ExtractStringLiteralValue(groupArgument.Expression);
        }
        catch (ArgumentException ex)
        {
            // If we can't parse the attribute arguments due to invalid argument format, return null
            Debug.WriteLine($"[Sailfish.TestAdapter] Failed to parse SailfishComparison attribute arguments: {ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            // If we can't access the expression due to invalid syntax tree state, return null
            Debug.WriteLine($"[Sailfish.TestAdapter] Failed to access SailfishComparison attribute expression: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts a string literal value from an expression.
    /// </summary>
    /// <param name="expression">The expression to extract from.</param>
    /// <returns>The string value, or null if not a string literal.</returns>
    private static string? ExtractStringLiteralValue(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal && literal.Token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralToken))
        {
            return literal.Token.ValueText;
        }
        return null;
    }

    /// <summary>
    /// Extracts an enum value from an expression.
    /// </summary>
    /// <param name="expression">The expression to extract from.</param>
    /// <returns>The enum value as a string, or null if not an enum member access.</returns>
    private static string? ExtractEnumValue(ExpressionSyntax expression)
    {
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.ValueText;
        }
        return null;
    }
}