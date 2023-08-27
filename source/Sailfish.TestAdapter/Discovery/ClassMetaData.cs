using System;
using Microsoft.CodeAnalysis;

namespace Sailfish.TestAdapter.Discovery;

public sealed class ClassMetaData
{
    public ClassMetaData(string filePath, string className, Type performanceTestType, SyntaxTree syntaxTree, MethodMetaData[] methods)
    {
        FilePath = filePath;
        ClassName = className;
        PerformanceTestType = performanceTestType;
        SyntaxTree = syntaxTree;
        Methods = methods;
    }

    public string FilePath { get; set; }
    public string ClassName { get; set; }
    public Type PerformanceTestType { get; }

    public SyntaxTree SyntaxTree { get; set; }
    public MethodMetaData[] Methods { get; set; }
}