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
                            .Any(attr => IsRunnableMethodAttribute(attr.Name.ToString(), methodAttributePrefix)));

                    // var className = classDeclaration.Identifier.ValueText;
                    var classFullName = RetrieveClassFullName(classDeclaration);
                    var performanceTestType = performanceTestTypes.SingleOrDefault(x => x.FullName == classFullName);
                    if (performanceTestType is null) continue;

                    // Class-level: does [Sailfish(DisableComparison = true)] suppress the implicit group?
                    var classDisablesComparison = ClassDisablesComparison(classDeclaration);

                    var classMetaData = new ClassMetaData(
                        classFullName: classFullName,
                        performanceTestType: performanceTestType,
                        filePath: filePath,
                        methods: (
                            from methodDeclaration in methodDeclarations
                            let lineSpan = syntaxTree.GetLineSpan(methodDeclaration.Span)
                            let lineNumber = lineSpan.StartLinePosition.Line + 1
                            let comparisonGroup = IsTrawlMethod(methodDeclaration) ? null : ExtractComparisonInfo(methodDeclaration, classFullName, classDisablesComparison)
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

    /// <summary>
    /// Both [SailfishMethod] microbenchmarks and [Trawl] load scenarios are runnable test cases that the
    /// adapter should surface to VSTest.
    /// </summary>
    private static bool IsRunnableMethodAttribute(string attributeName, string methodAttributePrefix)
    {
        return attributeName == methodAttributePrefix
               || attributeName == $"{methodAttributePrefix}Attribute"
               || attributeName == "Trawl"
               || attributeName == "TrawlAttribute";
    }

    private static bool IsTrawlMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .Any(attr => attr.Name.ToString() == "Trawl" || attr.Name.ToString() == "TrawlAttribute");
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
    /// Sentinel prefix used to encode the implicit class-wide comparison group as a property string.
    /// Embedding the class's full name keeps per-class implicit groups distinct when batched downstream.
    /// </summary>
    internal const string ImplicitComparisonGroupPrefix = "__implicit::";

    /// <summary>
    /// Extracts the comparison-group label for a method:
    ///   <list type="bullet">
    ///     <item><description>The explicit <c>ComparisonGroup</c> when set on <c>[SailfishMethod]</c>.</description></item>
    ///     <item><description><c>"__implicit::{ClassFullName}"</c> when the method has no explicit group and the enclosing class does not set <c>[Sailfish(DisableComparison = true)]</c>.</description></item>
    ///     <item><description><c>null</c> when neither applies (class has opted out of comparison and the method has no explicit group).</description></item>
    ///   </list>
    /// </summary>
    private static string? ExtractComparisonInfo(MethodDeclarationSyntax methodDeclaration, string classFullName, bool classDisablesComparison)
    {
        try
        {
            // Explicit ComparisonGroup on [SailfishMethod] wins regardless of class-level setting.
            var sailfishMethodAttr = methodDeclaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .FirstOrDefault(attr =>
                    attr.Name.ToString() == "SailfishMethod" ||
                    attr.Name.ToString() == "SailfishMethodAttribute");

            if (sailfishMethodAttr?.ArgumentList?.Arguments != null)
            {
                foreach (var arg in sailfishMethodAttr.ArgumentList.Arguments)
                {
                    if (arg.NameEquals?.Name.Identifier.ValueText == "ComparisonGroup")
                    {
                        var value = ExtractStringLiteralValue(arg.Expression);
                        if (!string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
            }

            // No explicit group → join the implicit class-wide group unless the class opts out.
            return classDisablesComparison ? null : ImplicitComparisonGroupPrefix + classFullName;
        }
        catch (ArgumentException ex)
        {
            Debug.WriteLine($"[Sailfish.TestAdapter] Failed to parse comparison attribute arguments: {ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[Sailfish.TestAdapter] Failed to access comparison attribute expression: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns true when the class's <c>[Sailfish]</c> attribute sets <c>DisableComparison = true</c>.
    /// </summary>
    private static bool ClassDisablesComparison(ClassDeclarationSyntax classDeclaration)
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
            if (arg.Expression is LiteralExpressionSyntax lit && lit.Token.IsKind(SyntaxKind.TrueKeyword))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Extracts a string literal value from an expression.
    /// </summary>
    /// <param name="expression">The expression to extract from.</param>
    /// <returns>The string value, or null if not a string literal.</returns>
    private static string? ExtractStringLiteralValue(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
        {
            return literal.Token.ValueText;
        }
        return null;
    }
}