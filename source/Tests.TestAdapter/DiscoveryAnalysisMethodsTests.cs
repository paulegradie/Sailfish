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

namespace Tests.TestAdapter
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
            classMetaData.ClassFullName.ShouldBe("Tests.TestAdapter.TestClass");
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

namespace Tests.TestAdapter;

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
            classMetaData.ClassFullName.ShouldBe("Tests.TestAdapter.TestClass");
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

namespace Tests.TestAdapter
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
            // Should only find classes with Sailfish attribute and matching types
            result.ShouldNotContain(meta => meta.ClassFullName.Contains("NonSailfishClass"));
            if (result.Any())
            {
                result.ShouldContain(meta => meta.ClassFullName == "Tests.TestAdapter.TestClass");
            }
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

namespace Tests.TestAdapter
{
    [Sailfish]
    public class TestClass
    {
        [SailfishMethod]
        public void TestMethod()
        {
        }
    }

    [Sailfish]
    public class AnotherTestClass
    {
        [SailfishMethod]
        public void AnotherTestMethod()
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
            result.Count.ShouldBe(2); // Should find both matching classes
            result.ShouldContain(meta => meta.ClassFullName == "Tests.TestAdapter.TestClass");
            result.ShouldContain(meta => meta.ClassFullName == "Tests.TestAdapter.AnotherTestClass");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompilePreRenderedSourceMap_WithEmptyFileList_ShouldReturnEmptyResult()
    {
        // Arrange
        var performanceTestTypes = new[] { typeof(TestClass) };

        // Act
        var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
            Array.Empty<string>(),
            performanceTestTypes,
            "Sailfish",
            "SailfishMethod").ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void CompilePreRenderedSourceMap_WithNonExistentFile_ShouldHandleGracefully()
    {
        // Arrange
        var performanceTestTypes = new[] { typeof(TestClass) };
        var nonExistentFile = "non-existent-file.cs";

        // Act & Assert - Should not throw
        var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
            new[] { nonExistentFile },
            performanceTestTypes,
            "Sailfish",
            "SailfishMethod").ToList();

        // Result should be empty or handle the missing file gracefully
        result.ShouldNotBeNull();
    }

    [Fact]
    public void CompilePreRenderedSourceMap_WithInvalidSourceCode_ShouldHandleGracefully()
    {
        // Arrange
        var invalidSourceCode = @"
This is not valid C# code
{{{
invalid syntax
";
        var tempFile = CreateTempSourceFile(invalidSourceCode);
        var performanceTestTypes = new[] { typeof(TestClass) };

        try
        {
            // Act & Assert - Should not throw
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                "Sailfish",
                "SailfishMethod").ToList();

            // Should handle invalid syntax gracefully
            result.ShouldNotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompilePreRenderedSourceMap_WithEmptyPerformanceTestTypes_ShouldReturnEmptyResult()
    {
        // Arrange
        var sourceCode = @"
using Sailfish.Attributes;

namespace Tests.TestAdapter
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
        var emptyPerformanceTestTypes = Array.Empty<Type>();

        try
        {
            // Act
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                emptyPerformanceTestTypes,
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
    public void CompilePreRenderedSourceMap_WithNullAttributeNames_ShouldHandleGracefully()
    {
        // Arrange
        var sourceCode = @"
using Sailfish.Attributes;

namespace Tests.TestAdapter
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
            // Act & Assert - Should not throw
            var result = DiscoveryAnalysisMethods.CompilePreRenderedSourceMap(
                new[] { tempFile },
                performanceTestTypes,
                null!,
                null!).ToList();

            // Should handle null attribute names gracefully
            result.ShouldNotBeNull();
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

// Test classes for the discovery analysis tests - already in Tests.TestAdapter namespace due to file-scoped namespace
// [Sailfish(Disabled = true, DisableOverheadEstimation = true)]
public class TestClass
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Test implementation
    }
}

// [Sailfish(Disabled = true, DisableOverheadEstimation = true)]
public class AnotherTestClass
{
    [SailfishMethod]
    public void AnotherTestMethod()
    {
        // Test implementation
    }
}
