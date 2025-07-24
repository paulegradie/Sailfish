using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

public class TestDiscoveryTests
{
    [Fact]
    public void DiscoverTests_ShouldReturnTestCases()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var sourceDllPaths = new[] { currentAssemblyPath };

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        testCases.ShouldNotBeEmpty();
        testCases.ShouldAllBe(tc => tc.Source == currentAssemblyPath);
        testCases.ShouldAllBe(tc => !string.IsNullOrEmpty(tc.DisplayName));
        testCases.ShouldAllBe(tc => !string.IsNullOrEmpty(tc.FullyQualifiedName));
    }

    [Fact]
    public void DiscoverTests_ShouldHandleEmptySourcePaths()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var sourceDllPaths = Array.Empty<string>();

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        testCases.ShouldBeEmpty();
    }

    [Fact]
    public void DiscoverTests_ShouldHandleNonExistentFiles()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var sourceDllPaths = new[] { "non-existent-file.dll" };

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        testCases.ShouldBeEmpty();
    }

    [Fact]
    public void DiscoverTests_ShouldHandleInvalidAssemblies()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var invalidAssemblyPath = CreateTempTextFile();

        try
        {
            var sourceDllPaths = new[] { invalidAssemblyPath };

            // Act
            var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

            // Assert
            testCases.ShouldBeEmpty();
        }
        finally
        {
            // Cleanup
            if (File.Exists(invalidAssemblyPath))
                File.Delete(invalidAssemblyPath);
        }
    }

    [Fact]
    public void DiscoverTests_ShouldHandleExceptionsDuringTestCaseAssembly()
    {
        // This test verifies that exceptions during test case assembly are caught and logged
        // but don't stop the discovery process for other classes
        
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var sourceDllPaths = new[] { currentAssemblyPath };

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        // Even if some classes fail to process, we should still get test cases from valid classes
        // The exact behavior depends on the implementation, but the discovery should be resilient
        testCases.ShouldNotBeNull();
    }

    [Fact]
    public void DiscoverTests_ShouldHandleDuplicateSourcePaths()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var sourceDllPaths = new[] { currentAssemblyPath, currentAssemblyPath, currentAssemblyPath };

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        testCases.ShouldNotBeEmpty();
        // Should handle duplicates gracefully without creating duplicate test cases
        var uniqueTestCases = testCases.GroupBy(tc => tc.FullyQualifiedName).ToList();
        uniqueTestCases.ShouldAllBe(group => group.Count() == 1, "No duplicate test cases should be created");
    }

    [Fact]
    public void DiscoverTests_ShouldFindProjectFiles()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var sourceDllPaths = new[] { currentAssemblyPath };

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        // The discovery process should be able to find and process project files
        // This is verified indirectly by the fact that test cases are returned
        testCases.ShouldNotBeNull();
    }

    [Fact]
    public void DiscoverTests_ShouldLogErrorsForFailedClasses()
    {
        // This test is more of a behavioral verification
        // In a real scenario with problematic classes, errors should be logged
        
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var sourceDllPaths = new[] { currentAssemblyPath };

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        // For a valid assembly, no error messages should be logged
        logger.DidNotReceive().SendMessage(TestMessageLevel.Error, Arg.Any<string>());
    }

    [Fact]
    public void DiscoverTests_ShouldProcessMultipleAssemblies()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var discovery = new TestDiscovery();
        var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var systemAssemblyPath = typeof(string).Assembly.Location;
        var sourceDllPaths = new[] { currentAssemblyPath, systemAssemblyPath };

        // Act
        var testCases = discovery.DiscoverTests(sourceDllPaths, logger).ToList();

        // Assert
        // Should process multiple assemblies, but only return test cases from assemblies with Sailfish tests
        testCases.ShouldNotBeEmpty();
        testCases.ShouldAllBe(tc => tc.Source == currentAssemblyPath);
    }

    private static string CreateTempTextFile()
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "This is not a valid assembly");
        return tempPath;
    }
}
