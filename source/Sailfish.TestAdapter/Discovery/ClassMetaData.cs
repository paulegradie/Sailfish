using Microsoft.CodeAnalysis;

namespace Sailfish.TestAdapter.Discovery;

public sealed class ClassMetaData
{
    public ClassMetaData(string filePath, string className, SyntaxTree syntaxTree, MethodMetaData[] methods)
    {
        FilePath = filePath;
        ClassName = className;
        SyntaxTree = syntaxTree;
        Methods = methods;
    }

    public string FilePath { get; set; }
    public string ClassName { get; set; }

    public SyntaxTree SyntaxTree { get; set; }
    public MethodMetaData[] Methods { get; set; }
}