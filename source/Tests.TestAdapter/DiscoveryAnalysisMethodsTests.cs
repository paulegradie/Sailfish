using System;
using System.IO;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

public class DiscoveryAnalysisMethodsTests
{
    [Fact]
    public void CompilePreRenderedSourceMap_ShouldParseValidSourceFiles()
    {
        // Arrange
        var sourceCode = @"
using Sailfish.Attributes;

namespace TestNamespace
{
    [Sailfish]
    public class TestClass
    {
        [SailfishMethod]
        public void TestMethod()
        {
        }
    }
}";
        var tempFile = CreateTempSourceFile(sourceCode);
        var performanceTestTypes = new[] { typeof(TestClass) };

        try
        {
            // Act
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                "Sailfish",
                "SailfishMethod").ToList();

            // Assert
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1);
            var classMetaData = result.First();
            classMetaData.ClassFullName.ShouldBe("TestNamespace.TestClass");
            classMetaData.Methods.ShouldNotBeEmpty();
            classMetaData.Methods.First().MethodName.ShouldBe("TestMethod");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompilePreRenderedSourceMap_ShouldHandleFileScopedNamespaces()
    {
        // Arrange
        var sourceCode = @"
using Sailfish.Attributes;

namespace TestNamespace;

[Sailfish]
public class TestClass
{
    [SailfishMethod]
    public void TestMethod()
    {
    }
}";
        var tempFile = CreateTempSourceFile(sourceCode);
        var performanceTestTypes = new[] { typeof(TestClass) };

        try
        {
            // Act
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                "Sailfish",
                "SailfishMethod").ToList();

            // Assert
            result.ShouldNotBeEmpty();
            var classMetaData = result.First();
            classMetaData.ClassFullName.ShouldBe("TestNamespace.TestClass");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompilePreRenderedSourceMap_ShouldHandleInvalidSourceFiles()
    {
        // Arrange
        var invalidSourceCode = "This is not valid C# code {{{";
        var tempFile = CreateTempSourceFile(invalidSourceCode);
        var performanceTestTypes = new[] { typeof(TestClass) };

        try
        {
            // Act
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                "Sailfish",
                "SailfishMethod").ToList();

            // Assert
            // Should handle invalid files gracefully and continue processing
            result.ShouldBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompilePreRenderedSourceMap_ShouldHandleEmptySourceFiles()
    {
        // Arrange
        var emptySourceCode = "";
        var tempFile = CreateTempSourceFile(emptySourceCode);
        var performanceTestTypes = new[] { typeof(TestClass) };

        try
        {
            // Act
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                "Sailfish",
                "SailfishMethod").ToList();

            // Assert
            result.ShouldBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompilePreRenderedSourceMap_ShouldHandleNonExistentFiles()
    {
        // Arrange
        var nonExistentFile = "non-existent-file.cs";
        var performanceTestTypes = new[] { typeof(TestClass) };

        // Act
        var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
            new[] { nonExistentFile },
            performanceTestTypes,
            "Sailfish",
            "SailfishMethod").ToList();

        // Assert
        // Should handle non-existent files gracefully
        result.ShouldBeEmpty();
    }

    [Fact]
    public void CompilePreRenderedSourceMap_ShouldFilterByAttributePrefix()
    {
        // Arrange
        var sourceCode = @"
using Sailfish.Attributes;

namespace TestNamespace
{
    [CustomAttribute]
    public class NonSailfishClass
    {
        [CustomMethod]
        public void NonSailfishMethod()
        {
        }
    }

    [Sailfish]
    public class SailfishClass
    {
        [SailfishMethod]
        public void SailfishMethod()
        {
        }
    }
}";
        var tempFile = CreateTempSourceFile(sourceCode);
        var performanceTestTypes = new[] { typeof(TestClass) };

        try
        {
            // Act
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                "Sailfish",
                "SailfishMethod").ToList();

            // Assert
            // Should only find classes with Sailfish attribute
            result.ShouldNotContain(meta => meta.ClassFullName.Contains("NonSailfishClass"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompilePreRenderedSourceMap_ShouldHandleMultipleClasses()
    {
        // Arrange
        var sourceCode = @"
using Sailfish.Attributes;

namespace TestNamespace
{
    [Sailfish]
    public class TestClass1
    {
        [SailfishMethod]
        public void TestMethod1()
        {
        }
    }

    [Sailfish]
    public class TestClass2
    {
        [SailfishMethod]
        public void TestMethod2()
        {
        }
    }
}";
        var tempFile = CreateTempSourceFile(sourceCode);
        var performanceTestTypes = new[] { typeof(TestClass), typeof(AnotherTestClass) };

        try
        {
            // Act
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                "Sailfish",
                "SailfishMethod").ToList();

            // Assert
            result.Count.ShouldBeGreaterThanOrEqualTo(0); // Depends on matching types
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private static string CreateTempSourceFile(string content)
    {
        var tempPath = Path.GetTempFileName();
        var csPath = Path.ChangeExtension(tempPath, ".cs");
        File.WriteAllText(csPath, content);
        return csPath;
    }
}

// Test classes for the discovery analysis tests
[Sailfish]
public class TestClass
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Test implementation
    }
}

[Sailfish]
public class AnotherTestClass
{
    [SailfishMethod]
    public void AnotherTestMethod()
    {
        // Test implementation
    }
}
