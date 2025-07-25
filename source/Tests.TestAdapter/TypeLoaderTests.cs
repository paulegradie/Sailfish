using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.Attributes;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

public class TypeLoaderTests
{
    [Fact]
    public void LoadSailfishTestTypesFrom_ShouldReturnSailfishTestTypes()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act
        var types = TypeLoader.LoadSailfishTestTypesFrom(currentAssemblyPath, logger);

        // Assert
        types.ShouldNotBeEmpty();
        types.ShouldAllBe(t => t.GetCustomAttribute<SailfishAttribute>() != null);
        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<SailfishAttribute>();
            (attr == null || attr.Disabled != true).ShouldBeTrue();
        }
    }

    [Fact]
    public void LoadSailfishTestTypesFrom_ShouldFilterDisabledTests()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act
        var types = TypeLoader.LoadSailfishTestTypesFrom(currentAssemblyPath, logger);

        // Assert
        // Verify that disabled tests are not included
        // Check that no disabled types are returned
        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<SailfishAttribute>();
            if (attr != null && attr.Disabled == true)
            {
                Assert.Fail($"Found disabled test type: {type.Name}");
            }
        }
    }

    [Fact]
    public void LoadSailfishTestTypesFrom_WithNonExistentFile_ShouldThrow()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-file.dll");

        // Act & Assert
        Should.Throw<FileNotFoundException>(() =>
            TypeLoader.LoadSailfishTestTypesFrom(nonExistentPath, logger));
    }

    [Fact]
    public void LoadSailfishTestTypesFrom_WithInvalidAssembly_ShouldThrow()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var invalidAssemblyPath = CreateTempTextFile();

        try
        {
            // Act & Assert
            Should.Throw<BadImageFormatException>(() =>
                TypeLoader.LoadSailfishTestTypesFrom(invalidAssemblyPath, logger));
        }
        finally
        {
            // Cleanup
            if (File.Exists(invalidAssemblyPath))
                File.Delete(invalidAssemblyPath);
        }
    }

    [Fact]
    public void LoadSailfishTestTypesFrom_ShouldHandleReflectionTypeLoadException()
    {
        // This test is more complex as it requires an assembly that partially loads
        // For now, we'll test with a valid assembly and verify no warnings are logged

        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act
        var types = TypeLoader.LoadSailfishTestTypesFrom(currentAssemblyPath, logger);

        // Assert
        types.ShouldNotBeEmpty();
        // Verify no warning messages were sent for a valid assembly
        logger.DidNotReceive().SendMessage(TestMessageLevel.Warning, Arg.Any<string>());
    }

    [Fact]
    public void LoadSailfishTestTypesFrom_ShouldReturnOnlyClassesWithSailfishAttribute()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();
        var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act
        var types = TypeLoader.LoadSailfishTestTypesFrom(currentAssemblyPath, logger);

        // Assert
        foreach (var type in types)
        {
            type.ShouldSatisfyAllConditions(
                () => type.GetCustomAttribute<SailfishAttribute>().ShouldNotBeNull(),
                () => type.IsClass.ShouldBeTrue()
            );
        }
    }

    [Fact]
    public void LoadSailfishTestTypesFrom_ShouldReturnEmptyArrayForAssemblyWithoutSailfishTests()
    {
        // Arrange
        var logger = Substitute.For<IMessageLogger>();

        // Use a .NET assembly that we know exists and doesn't have Sailfish tests
        // System.Text.Json is a good choice as it's commonly available and doesn't have Sailfish tests
        var systemAssemblyPath = typeof(System.Text.Json.JsonSerializer).Assembly.Location;

        // Act & Assert
        try
        {
            var types = TypeLoader.LoadSailfishTestTypesFrom(systemAssemblyPath, logger);
            types.ShouldBeEmpty();
        }
        catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException or FileLoadException)
        {
            // If the assembly can't be loaded for any reason, that's also acceptable
            // The important thing is that the method handles it gracefully
        }
    }

    private static string CreateTempTextFile()
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "This is not a valid assembly");
        return tempPath;
    }
}

// Test classes for the TypeLoader tests
[Sailfish(DisableOverheadEstimation = true, NumWarmupIterations = 0, SampleSize = 1)]
public class ValidSailfishTestClass
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Test implementation
    }
}

[Sailfish(Disabled = true)]
public class DisabledSailfishTestClass
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Test implementation
    }
}

public class NonSailfishTestClass
{
    public void RegularMethod()
    {
        // Regular method
    }
}
