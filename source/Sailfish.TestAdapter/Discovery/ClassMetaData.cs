using System;
using Microsoft.CodeAnalysis;

namespace Sailfish.TestAdapter.Discovery;

public sealed class ClassMetaData
{
    public ClassMetaData(string filePath, string classFullName, Type performanceTestType, SyntaxTree syntaxTree, MethodMetaData[] methods)
    {
        FilePath = filePath;
        ClassFullName = classFullName;
        PerformanceTestType = performanceTestType;
        SyntaxTree = syntaxTree;
        Methods = methods;
    }

    public string FilePath { get; set; }
    public string ClassFullName { get; set; }
    public Type PerformanceTestType { get; }

    public SyntaxTree SyntaxTree { get; set; }
    public MethodMetaData[] Methods { get; set; }
}