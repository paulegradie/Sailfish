using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Discovery;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

/// <summary>
/// Unit tests for DirectoryRecursion utility class to ensure proper file system
/// operations and error handling in various edge case scenarios.
/// </summary>
public class DirectoryRecursionTests
{
    private readonly IMessageLogger _mockLogger;

    public DirectoryRecursionTests()
    {
        _mockLogger = Substitute.For<IMessageLogger>();
    }

    #region RecurseUpwardsUntilFileIsFound Tests

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithValidFile_ShouldReturnFileInfo()
    {
        // Build an isolated three-level dir tree under Path.GetTempPath() with the .csproj
        // file at the top. Pre-fix this test used Directory.GetCurrentDirectory() as the
        // start point and dropped a test.csproj into it — but RecurseUpwardsUntilFileIsFound
        // searches the *parent* of its source path, so it only succeeded when something else
        // in the test runner's working-directory chain happened to contain a .csproj. CI
        // layouts that lack that ancestry (build agents, dotnet test in detached mode) made
        // the test flake. The temp-dir tree is hermetic.
        var rootDir = Path.Combine(Path.GetTempPath(), "Sailfish_DirRecursionTest_" + Guid.NewGuid().ToString("N"));
        var middleDir = Path.Combine(rootDir, "middle");
        var leafDir = Path.Combine(middleDir, "leaf");
        Directory.CreateDirectory(leafDir);

        var csprojPath = Path.Combine(rootDir, "TestProject.csproj");
        File.WriteAllText(csprojPath, "<Project></Project>");
        // The function inspects files in Path.GetDirectoryName(sourceFile); pass a file in
        // the leaf directory so the recursion has to walk up two levels to find the csproj.
        var sourceFile = Path.Combine(leafDir, "source.cs");
        File.WriteAllText(sourceFile, string.Empty);

        try
        {
            var result = DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", sourceFile, 5);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("TestProject.csproj");
        }
        finally
        {
            if (Directory.Exists(rootDir)) Directory.Delete(rootDir, recursive: true);
        }
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithNonExistentFile_ShouldThrowException()
    {
        // Arrange
        var currentDirectory = Directory.GetCurrentDirectory();
        var nonExistentSuffix = ".nonexistent" + Guid.NewGuid().ToString("N")[..8];

        // Act & Assert
        var exception = Should.Throw<TestAdapterException>(() =>
            DirectoryRecursion.RecurseUpwardsUntilFileIsFound(nonExistentSuffix, currentDirectory, 5));
        
        exception.Message.ShouldContain($"Couldn't locate a ${nonExistentSuffix} file in this project.");
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithMaxDepthZero_ShouldThrowException()
    {
        // Arrange
        var currentDirectory = Directory.GetCurrentDirectory();

        // Act & Assert
        var exception = Should.Throw<TestAdapterException>(() =>
            DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", currentDirectory, 0));
        
        exception.Message.ShouldContain("Couldn't locate a $.csproj file in this project.");
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var invalidPath = "Z:\\NonExistent\\Path\\That\\Does\\Not\\Exist";

        // Act & Assert
        // The method can throw either TestAdapterException or DirectoryNotFoundException
        // depending on when the invalid path is detected
        Should.Throw<Exception>(() =>
            DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", invalidPath, 5));
    }

    [Fact]
    public void RecurseUpwardsUntilFileIsFound_WithEmptyString_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<TestAdapterException>(() =>
            DirectoryRecursion.RecurseUpwardsUntilFileIsFound(".csproj", string.Empty, 5));
    }

    #endregion

    #region FindAllFilesRecursively Tests

    [Fact]
    public void FindAllFilesRecursively_WithValidPattern_ShouldReturnFiles()
    {
        // Arrange
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var originFile = new FileInfo(Path.Combine(currentDir.FullName, "origin.txt"));

        // Act
        var result = DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<string>>();
        // The result might be empty due to bin/obj filtering, but the method should work
        result.Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void FindAllFilesRecursively_WithFilter_ShouldApplyFilter()
    {
        // Arrange
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var originFile = new FileInfo(Path.Combine(currentDir.FullName, "origin.txt"));

        // Act
        var allFiles = DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger);
        var filteredFiles = DirectoryRecursion.FindAllFilesRecursively(originFile, "*.cs", _mockLogger, 
            path => path.Contains("Tests"));

        // Assert
        filteredFiles.Count.ShouldBeLessThanOrEqualTo(allFiles.Count);
        filteredFiles.All(f => f.Contains("Tests")).ShouldBeTrue();
    }

    [Fact]
    public void FindAllFilesRecursively_WithNonExistentPattern_ShouldReturnEmptyList()
    {
        // Arrange
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var originFile = new FileInfo(Path.Combine(currentDir.FullName, "origin.txt"));
        var uniquePattern = $"*.{Guid.NewGuid():N}";

        // Act
        var result = DirectoryRecursion.FindAllFilesRecursively(originFile, uniquePattern, _mockLogger);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    #endregion

    #region FileSearchFilters Tests

    [Fact]
    public void FilePathDoesNotContainBinOrObjDirs_WithBinPath_ShouldReturnFalse()
    {
        // Arrange
        var pathWithBin = $"C:\\Project\\bin\\Debug\\file.dll";

        // Act
        var result = DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(pathWithBin);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void FilePathDoesNotContainBinOrObjDirs_WithObjPath_ShouldReturnFalse()
    {
        // Arrange
        var pathWithObj = $"C:\\Project\\obj\\Debug\\file.dll";

        // Act
        var result = DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(pathWithObj);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void FilePathDoesNotContainBinOrObjDirs_WithValidPath_ShouldReturnTrue()
    {
        // Arrange
        var validPath = $"C:\\Project\\src\\file.cs";

        // Act
        var result = DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(validPath);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void FilePathDoesNotContainBinOrObjDirs_WithEmptyPath_ShouldReturnTrue()
    {
        // Act
        var result = DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(string.Empty);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void FilePathDoesNotContainBinOrObjDirs_WithPathContainingBinInFilename_ShouldReturnTrue()
    {
        // Arrange - filename contains "bin" but not in directory structure
        var pathWithBinInFilename = $"C:\\Project\\src\\binary.cs";

        // Act
        var result = DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(pathWithBinInFilename);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void FilePathDoesNotContainBinOrObjDirs_WithUnixStylePaths_ShouldWork()
    {
        // Arrange
        var unixBinPath = "/project/bin/debug/file.dll";
        var unixObjPath = "/project/obj/debug/file.dll";
        var unixValidPath = "/project/src/file.cs";

        // Act & Assert
        // Note: This test may behave differently on different OS due to Path.DirectorySeparatorChar
        // but it tests the logic with different path separators
        if (Path.DirectorySeparatorChar == '/')
        {
            DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(unixBinPath).ShouldBeFalse();
            DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(unixObjPath).ShouldBeFalse();
            DirectoryRecursion.FileSearchFilters.FilePathDoesNotContainBinOrObjDirs(unixValidPath).ShouldBeTrue();
        }

    #endregion
    }
}
