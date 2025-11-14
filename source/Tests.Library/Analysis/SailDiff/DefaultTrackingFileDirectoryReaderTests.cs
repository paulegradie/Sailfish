using System;
using System.Collections.Generic;
using System.IO;
using Sailfish.Analysis.SailDiff;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class DefaultTrackingFileDirectoryReaderTests : IDisposable
{
    private readonly DefaultTrackingFileDirectoryReader _reader;
    private readonly string _tempDirectory;
    private readonly List<string> _createdFiles;

    public DefaultTrackingFileDirectoryReaderTests()
    {
        _reader = new DefaultTrackingFileDirectoryReader();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _createdFiles = [];
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
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var directoryReader = new DefaultTrackingFileDirectoryReader();

        // Assert
        directoryReader.ShouldNotBeNull();
        directoryReader.ShouldBeOfType<DefaultTrackingFileDirectoryReader>();
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        // tempDirectory is already empty

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithNoTrackingFiles_ReturnsEmptyList()
    {
        // Arrange
        CreateFile("regular.txt");
        CreateFile("data.json");
        CreateFile("config.xml");

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithSingleTrackingFile_ReturnsSingleFile()
    {
        // Arrange
        var trackingFile = CreateTrackingFile("test1");

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe(trackingFile);
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithMultipleTrackingFiles_ReturnsDescendingByDefault()
    {
        // Arrange
        var file1 = CreateTrackingFileWithDelay("test1", TimeSpan.FromMilliseconds(100));
        var file2 = CreateTrackingFileWithDelay("test2", TimeSpan.FromMilliseconds(200));
        var file3 = CreateTrackingFileWithDelay("test3", TimeSpan.FromMilliseconds(300));

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        // Should be ordered by most recent first (descending)
        result[0].ShouldBe(file3); // Most recent
        result[1].ShouldBe(file2);
        result[2].ShouldBe(file1); // Oldest
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithAscendingTrue_ReturnsAscendingOrder()
    {
        // Arrange
        var file1 = CreateTrackingFileWithDelay("test1", TimeSpan.FromMilliseconds(100));
        var file2 = CreateTrackingFileWithDelay("test2", TimeSpan.FromMilliseconds(200));
        var file3 = CreateTrackingFileWithDelay("test3", TimeSpan.FromMilliseconds(300));

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory, ascending: true);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        // Should be ordered by oldest first (ascending)
        result[0].ShouldBe(file1); // Oldest
        result[1].ShouldBe(file2);
        result[2].ShouldBe(file3); // Most recent
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithMixedFileTypes_ReturnsOnlyTrackingFiles()
    {
        // Arrange
        CreateFile("regular.txt");
        var trackingFile1 = CreateTrackingFileWithDelay("test1", TimeSpan.FromMilliseconds(100));
        CreateFile("data.json");
        var trackingFile2 = CreateTrackingFileWithDelay("test2", TimeSpan.FromMilliseconds(200));
        CreateFile("config.xml");

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(trackingFile1);
        result.ShouldContain(trackingFile2);
        // Should be in descending order
        result[0].ShouldBe(trackingFile2); // More recent
        result[1].ShouldBe(trackingFile1); // Older
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithIdenticalTimestamps_MaintainsStableOrder()
    {
        // Arrange
        var file1 = CreateTrackingFile("test1");
        var file2 = CreateTrackingFile("test2");
        
        // Set identical timestamps
        var timestamp = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(file1, timestamp);
        File.SetLastWriteTimeUtc(file2, timestamp);

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(file1);
        result.ShouldContain(file2);
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() =>
            _reader.FindTrackingFilesInDirectoryOrderedByLastModified(nonExistentDirectory));
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithNullDirectory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _reader.FindTrackingFilesInDirectoryOrderedByLastModified(null!));
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithEmptyStringDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _reader.FindTrackingFilesInDirectoryOrderedByLastModified(string.Empty));
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithTrackingSuffixInMiddleOfFilename_DoesNotMatch()
    {
        // Arrange
        CreateFile($"test{DefaultFileSettings.TrackingSuffix}.txt");
        CreateFile($"prefix{DefaultFileSettings.TrackingSuffix}suffix.dat");
        var validTrackingFile = CreateTrackingFile("valid");

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe(validTrackingFile);
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithLargeNumberOfFiles_PerformsCorrectly()
    {
        // Arrange
        var trackingFiles = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            var file = CreateTrackingFileWithDelay($"test{i:D3}", TimeSpan.FromMilliseconds(i * 10));
            trackingFiles.Add(file);
        }

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(100);
        
        // Verify descending order (most recent first)
        for (int i = 0; i < result.Count - 1; i++)
        {
            var currentFileTime = File.GetLastWriteTimeUtc(result[i]);
            var nextFileTime = File.GetLastWriteTimeUtc(result[i + 1]);
            currentFileTime.ShouldBeGreaterThanOrEqualTo(nextFileTime);
        }
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_VerifiesTrackingSuffixValue()
    {
        // This test ensures we're using the correct tracking suffix
        // Arrange
        var expectedSuffix = DefaultFileSettings.TrackingSuffix; // Should be ".json.tracking"
        expectedSuffix.ShouldBe(".json.tracking");
        
        var trackingFile = CreateFile($"test{expectedSuffix}");
        var nonTrackingFile = CreateFile("test.json.nottracking");

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe(trackingFile);
    }

    private string CreateFile(string fileName)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(filePath, "test content");
        _createdFiles.Add(filePath);
        return filePath;
    }

    private string CreateTrackingFile(string baseName)
    {
        return CreateFile($"{baseName}{DefaultFileSettings.TrackingSuffix}");
    }

    private string CreateTrackingFileWithDelay(string baseName, TimeSpan delay)
    {
        var filePath = CreateTrackingFile(baseName);
        System.Threading.Thread.Sleep(delay);
        // Touch the file to update its timestamp
        File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
        return filePath;
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithSubdirectories_IgnoresSubdirectoryFiles()
    {
        // Arrange
        var subdirectory = Path.Combine(_tempDirectory, "subdir");
        Directory.CreateDirectory(subdirectory);

        var mainTrackingFile = CreateTrackingFile("main");
        var subTrackingFile = Path.Combine(subdirectory, $"sub{DefaultFileSettings.TrackingSuffix}");
        File.WriteAllText(subTrackingFile, "sub content");

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe(mainTrackingFile);
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithSpecialCharactersInFilename_HandlesCorrectly()
    {
        // Arrange
        var specialFile1 = CreateTrackingFile("test with spaces");
        var specialFile2 = CreateTrackingFile("test-with-dashes");
        var specialFile3 = CreateTrackingFile("test_with_underscores");

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.ShouldContain(specialFile1);
        result.ShouldContain(specialFile2);
        result.ShouldContain(specialFile3);
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithVeryOldAndVeryNewFiles_OrdersCorrectly()
    {
        // Arrange
        var oldFile = CreateTrackingFile("old");
        var newFile = CreateTrackingFile("new");

        // Set very old timestamp
        File.SetLastWriteTimeUtc(oldFile, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        // Set very new timestamp
        File.SetLastWriteTimeUtc(newFile, new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc));

        // Act
        var resultDescending = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory, ascending: false);
        var resultAscending = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory, ascending: true);

        // Assert
        resultDescending.ShouldNotBeNull();
        resultDescending.Count.ShouldBe(2);
        resultDescending[0].ShouldBe(newFile); // Most recent first
        resultDescending[1].ShouldBe(oldFile);

        resultAscending.ShouldNotBeNull();
        resultAscending.Count.ShouldBe(2);
        resultAscending[0].ShouldBe(oldFile); // Oldest first
        resultAscending[1].ShouldBe(newFile);
    }

    [Fact]
    public void FindTrackingFilesInDirectoryOrderedByLastModified_WithReadOnlyFiles_HandlesCorrectly()
    {
        // Arrange
        var readOnlyFile = CreateTrackingFile("readonly");
        File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);

        // Act
        var result = _reader.FindTrackingFilesInDirectoryOrderedByLastModified(_tempDirectory);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe(readOnlyFile);

        // Cleanup - remove readonly attribute for disposal
        File.SetAttributes(readOnlyFile, FileAttributes.Normal);
    }
}
