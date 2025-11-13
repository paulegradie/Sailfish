using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.Utils;

public class IsASailfishTestTypeExtensionMethodsTests
{
    private static (SemanticModel model, TypeDeclarationSyntax typeDecl) GetModelAndType(string code, string typeName)
    {
        var text = code.AddSailfishAttributeDependencies();
        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview));
        var refs = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create("TestAsm", new[] { tree }, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var typeDecl = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First(t => t.Identifier.Text == typeName);
        return (model, typeDecl);
    }

    [Fact]
    public void ReturnsTrue_ForTypeWithSailfishAttribute()
    {
        const string code = @"[Sailfish] public class Bench { }";
        var (model, typeDecl) = GetModelAndType(code, "Bench");
        Assert.True(typeDecl.IsASailfishTestType(model));
    }

    [Fact]
    public void ReturnsTrue_ForBaseTypeWhenDerivedHasSailfishAttribute()
    {
        const string code = @"public class Base { } [Sailfish] public class Derived : Base { }";
        var (model, baseDecl) = GetModelAndType(code, "Base");
        Assert.True(baseDecl.IsASailfishTestType(model));
    }

    [Fact]
    public void ReturnsFalse_WhenNoTypeHasSailfishAttribute()
    {
        const string code = @"public class A { } public class B : A { }";
        var (model, typeDecl) = GetModelAndType(code, "A");
        Assert.False(typeDecl.IsASailfishTestType(model));
    }
}

