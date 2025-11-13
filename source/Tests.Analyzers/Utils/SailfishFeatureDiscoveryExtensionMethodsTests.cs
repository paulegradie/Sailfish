using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sailfish.Analyzers.Utils.TreeParsingExtensionMethods;
using Tests.Analyzers.Utils;
using Xunit;

namespace Tests.Analyzers.Utils;

public class SailfishFeatureDiscoveryExtensionMethodsTests
{
    private static SemanticModel GetModel(string code, out SyntaxNode root)
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
        root = tree.GetRoot();
        return compilation.GetSemanticModel(tree);
    }

    [Fact]
    public void IdentifierName_IsSailfishVariable_ReturnsTrue()
    {
        var id = SyntaxFactory.IdentifierName("SailfishVariable");
        Assert.True(id.IsSailfishVariableProperty());
        Assert.False(SyntaxFactory.IdentifierName("Other").IsSailfishVariableProperty());
    }

    [Fact]
    public void PropertyDeclaration_IsSailfishVariable_ReturnsTrue()
    {
        var model = GetModel("[Sailfish] public class C { [SailfishVariable(1)] public int P { get; set; } }", out var root);
        var cls = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "C");
        var prop = cls.Members.OfType<PropertyDeclarationSyntax>().First();
        Assert.True(prop.IsSailfishVariableProperty());
    }

    [Fact]
    public void FindAllTypesInInheritanceTree_ByTypeDeclaration_CollectsAllDerived()
    {
        var model = GetModel("public class A { } public class B : A { } public class C : B { }", out var root);
        var baseDecl = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First(t => t.Identifier.Text == "A");
        var all = NodeExtensionMethods.FindAllTypesInInheritanceTree(baseDecl, model).Select(t => t.Identifier.Text).ToList();
        Assert.Equal(new[] { "B", "C" }.OrderBy(x => x), all.OrderBy(x => x));
    }

    [Fact]
    public void FindAllTypesInInheritanceTree_BySymbol_CollectsAllDerived()
    {
        var model = GetModel("public class A { } public class B : A { } public class C : B { }", out var root);
        var baseDecl = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First(t => t.Identifier.Text == "A");
        var sym = model.GetDeclaredSymbol(baseDecl)!;
        var all = NodeExtensionMethods.FindAllTypesInInheritanceTree(sym, model).Select(t => t.Identifier.Text).ToList();
        Assert.Equal(new[] { "B", "C" }.OrderBy(x => x), all.OrderBy(x => x));
    }

    [Fact]
    public void MethodAttributeHelpers_WorkAsExpected()
    {
        var model = GetModel("public class C { [SailfishGlobalSetup] public void M(){} [SailfishGlobalSetup, SailfishMethod] public void N(){} }", out var root);
        var cls = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "C");
        var methods = cls.Members.OfType<MethodDeclarationSyntax>().ToList();
        var m = methods[0];
        var n = methods[1];

        Assert.True(m.HasAttributesWithNames("SailfishGlobalSetup"));
        Assert.False(n.HasAttributesWithNames("SailfishGlobalSetup"));
        Assert.True(m.HasAttributeAmong(new[] { "SailfishGlobalSetup", "Other" }));
        Assert.Equal(new[] { "SailfishGlobalSetup", "SailfishMethod" }.OrderBy(x => x), n.GetAllAttributesAmong(new[] { "SailfishGlobalSetup", "SailfishMethod" }).OrderBy(x => x));
    }

    [Fact]
    public void PropertyAttributeHelper_Works()
    {
        var model = GetModel("public class C { [SailfishVariable(1)] public int X { get; set; } }", out var root);
        var cls = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "C");
        var prop = cls.Members.OfType<PropertyDeclarationSyntax>().First();
        Assert.True(prop.HasAttributesWithNames("SailfishVariable"));
        Assert.False(prop.HasAttributesWithNames("SailfishVariable", "Other"));
    }
}

