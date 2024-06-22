using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                    Console.WriteLine(ex.Message);
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
                    Console.WriteLine(ex.Message);
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
                            select new MethodMetaData(methodDeclaration.Identifier.ValueText, lineNumber))
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
            if (parentNode is FileScopedNamespaceDeclarationSyntax namespaceNode) fullClassName = $"{namespaceNode.Name}.{classDeclarationSyntax.Identifier.ValueText}";

            parentNode = parentNode.Parent;
        }

        return fullClassName;
    }
}