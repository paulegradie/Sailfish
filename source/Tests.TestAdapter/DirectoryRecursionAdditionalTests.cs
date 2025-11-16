using System;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

/// <summary>
/// Additional unit tests for DirectoryRecursion utility class to improve code coverage
/// by testing edge cases and error conditions not covered in the main test file.
/// </summary>
public class DirectoryRecursionAdditionalTests : IDisposable
{
    private readonly IMessageLogger _mockLogger;
    private readonly string _tempDirectory;

    public DirectoryRecursionAdditionalTests()
    {
        _mockLogger = Substitute.For<IMessageLogger>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"sailfish_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void FileSearchFilters_FilePathDoesNotContainBinOrObjDirs_WithMixedCasePaths_ShouldWork()
    {
        // Arrange
        var mixedCaseBinPath = $"C:\\Project\\BIN\\Debug\\file.dll";
        var mixedCaseObjPath = $"C:\\Project\\OBJ\\Debug\\file.dll";

        // Act & Assert
        // The filter is case-sensitive and looks for exact matches with separators
        DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(mixedCaseBinPath).ShouldBeTrue();
        DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(mixedCaseObjPath).ShouldBeTrue();
    }

    [Fact]
    public void FileSearchFilters_FilePathDoesNotContainBinOrObjDirs_WithNestedBinObjDirs_ShouldReturnFalse()
    {
        // Arrange
        var nestedBinPath = $"C:\\Project\\src\\bin\\nested\\bin\\file.dll";
        var nestedObjPath = $"C:\\Project\\src\\obj\\nested\\obj\\file.dll";

        // Act & Assert
        DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(nestedBinPath).ShouldBeFalse();
        DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(nestedObjPath).ShouldBeFalse();
    }

    [Fact]
    public void FindAllFilesRecursively_WithNonExistentDirectory_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_tempDirectory, "nonexistent");
        var originFile = new FileInfo(Path.Combine(nonExistentDir, "origin.txt"));

        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() =>
            DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger));
    }

    [Fact]
    public void FindAllFilesRecursively_WithFilterThatExcludesAll_ShouldReturnEmptyList()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.cs");
        File.WriteAllText(testFile, "// test file");
        
        var originFile = new FileInfo(Path.Combine(_tempDirectory, "origin.txt"));

        // Filter that excludes everything
        bool ExcludeAllFilter(string path) => false;

        // Act
        var result = DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger, ExcludeAllFilter);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindAllFilesRecursively_WithFilterThatIncludesSpecificFiles_ShouldReturnFilteredResults()
    {
        // Arrange
        var includeFile = Path.Combine(_tempDirectory, "include_me.cs");
        var excludeFile = Path.Combine(_tempDirectory, "exclude_me.cs");
        
        File.WriteAllText(includeFile, "// include this file");
        File.WriteAllText(excludeFile, "// exclude this file");
        
        var originFile = new FileInfo(Path.Combine(_tempDirectory, "origin.txt"));

        // Filter that only includes files with "include" in the name
        bool IncludeOnlyFilter(string path) => path.Contains("include");

        // Act
        var result = DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger, IncludeOnlyFilter);

        // Assert
        result.ShouldNotBeEmpty();
        result.ShouldContain(includeFile);
        result.ShouldNotContain(excludeFile);
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithFileInCurrentDirectory_ShouldReturnImmediately()
    {
        // Arrange
        var targetFile = Path.Combine(_tempDirectory, "target.csproj");
        var sourceFile = Path.Combine(_tempDirectory, "source.cs");
        
        File.WriteAllText(targetFile, "<Project></Project>");
        File.WriteAllText(sourceFile, "// source");

        // Act
        var result = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", sourceFile, 5);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("target.csproj");
        result.DirectoryName.ShouldBe(_tempDirectory);
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithMaxDepthReached_ShouldThrowException()
    {
        // Arrange
        var deepDir = _tempDirectory;
        for (var i = 0; i < 5; i++)
        {
            deepDir = Path.Combine(deepDir, $"level{i}");
            Directory.CreateDirectory(deepDir);
        }
        
        var sourceFile = Path.Combine(deepDir, "source.cs");
        File.WriteAllText(sourceFile, "// deep source");
        
        // Put the target file at the root, but set max depth too low
        var targetFile = Path.Combine(_tempDirectory, "target.csproj");
        File.WriteAllText(targetFile, "<Project></Project>");

        // Act & Assert
        Should.Throw<TestAdapterException>(() =>
            DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", sourceFile, 3));
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithMultipleMatchingFiles_ShouldReturnFirst()
    {
        // Arrange
        var subDir = Path.Combine(_tempDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        
        var rootTarget = Path.Combine(_tempDirectory, "root.csproj");
        var subTarget = Path.Combine(subDir, "sub.csproj");
        var sourceFile = Path.Combine(subDir, "source.cs");
        
        File.WriteAllText(rootTarget, "<Project>Root</Project>");
        File.WriteAllText(subTarget, "<Project>Sub</Project>");
        File.WriteAllText(sourceFile, "// source");

        // Act
        var result = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", sourceFile, 5);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("sub.csproj"); // Should find the closest one first
        result.DirectoryName.ShouldBe(subDir);
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithRootDirectory_ShouldHandleGracefully()
    {
        // Arrange
        var rootPath = Path.GetPathRoot(Environment.CurrentDirectory);
        if (rootPath == null) return; // Skip test if we can't get root path
        
        var sourceFile = Path.Combine(rootPath, "source.cs");
        
        // We don't actually create the file since we're testing at root level
        // This tests the boundary condition of reaching the root directory

        // Act & Assert
        Should.Throw<TestAdapterException>(() =>
            DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".nonexistent", sourceFile, 10));
    }

    [Fact]
    public void FindAllFilesRecursively_WithSubdirectories_ShouldSearchRecursively()
    {
        // Arrange
        var subDir1 = Path.Combine(_tempDirectory, "sub1");
        var subDir2 = Path.Combine(_tempDirectory, "sub2");
        var deepDir = Path.Combine(subDir1, "deep");
        
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);
        Directory.CreateDirectory(deepDir);
        
        var file1 = Path.Combine(_tempDirectory, "root.cs");
        var file2 = Path.Combine(subDir1, "sub1.cs");
        var file3 = Path.Combine(subDir2, "sub2.cs");
        var file4 = Path.Combine(deepDir, "deep.cs");
        
        File.WriteAllText(file1, "// root");
        File.WriteAllText(file2, "// sub1");
        File.WriteAllText(file3, "// sub2");
        File.WriteAllText(file4, "// deep");
        
        var originFile = new FileInfo(Path.Combine(_tempDirectory, "origin.txt"));

        // Act
        var result = DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger);

        // Assert
        result.Count.ShouldBe(4);
        result.ShouldContain(file1);
        result.ShouldContain(file2);
        result.ShouldContain(file3);
        result.ShouldContain(file4);
    }

    [Fact]
    public void FindAllFilesRecursively_WithNullFilter_ShouldReturnAllFiles()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.cs");
        File.WriteAllText(testFile, "// test file");
        
        var originFile = new FileInfo(Path.Combine(_tempDirectory, "origin.txt"));

        // Act
        var result = DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger, null);

        // Assert
        result.ShouldNotBeEmpty();
        result.ShouldContain(testFile);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
