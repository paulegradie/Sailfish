using System.Collections.Generic;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Extensions.Types;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class TrackingFileFinderTests
{
    private readonly ITrackingFileDirectoryReader _mockTrackingFileDirectoryReader;
    private readonly TrackingFileFinder _trackingFileFinder;

    public TrackingFileFinderTests()
    {
        _mockTrackingFileDirectoryReader = Substitute.For<ITrackingFileDirectoryReader>();
        _trackingFileFinder = new TrackingFileFinder(_mockTrackingFileDirectoryReader);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependency()
    {
        // Arrange & Act
        var finder = new TrackingFileFinder(_mockTrackingFileDirectoryReader);

        // Assert
        finder.ShouldNotBeNull();
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithNoFiles_ReturnsEmptyLists()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns([]);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldBeEmpty();
        result.AfterFilePaths.ShouldBeEmpty();
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithOneFile_ReturnsEmptyLists()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();
        var files = new List<string> { "file1.json.tracking" };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldBeEmpty();
        result.AfterFilePaths.ShouldBeEmpty();
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithTwoFiles_ReturnsCorrectBeforeAndAfter()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();
        var files = new List<string> { "file1.json.tracking", "file2.json.tracking" };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldContain("file2.json.tracking");
        result.AfterFilePaths.ShouldContain("file1.json.tracking");
        result.BeforeFilePaths.Count.ShouldBe(1);
        result.AfterFilePaths.Count.ShouldBe(1);
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithMultipleFiles_ReturnsCorrectBeforeAndAfter()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();
        var files = new List<string>
        {
            "file1.json.tracking", "file2.json.tracking", "file3.json.tracking", "file4.json.tracking"
        };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldContain("file2.json.tracking");
        result.AfterFilePaths.ShouldContain("file1.json.tracking");
        result.BeforeFilePaths.Count.ShouldBe(1);
        result.AfterFilePaths.Count.ShouldBe(1);
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithTagsAndMatchingFiles_FiltersCorrectly()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();
        tags.Add("Version", "1.0.0");
        tags.Add("Environment", "Test");

        var files = new List<string>
        {
            "file1.tags-Version=1.0.0__Environment=Test.json.tracking",
            "file2.tags-Version=1.0.0__Environment=Test.json.tracking",
            "file3.tags-Version=2.0.0__Environment=Prod.json.tracking"
        };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldContain("file2.tags-Version=1.0.0__Environment=Test.json.tracking");
        result.AfterFilePaths.ShouldContain("file1.tags-Version=1.0.0__Environment=Test.json.tracking");
        result.BeforeFilePaths.Count.ShouldBe(1);
        result.AfterFilePaths.Count.ShouldBe(1);
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithTagsAndNoMatchingFiles_ReturnsEmptyLists()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();
        tags.Add("Version", "1.0.0");

        var files = new List<string>
        {
            "file1.tags-Version=2.0.0.json.tracking", "file2.tags-Version=3.0.0.json.tracking"
        };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldBeEmpty();
        result.AfterFilePaths.ShouldBeEmpty();
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithNoTagsAndFilesWithTagsPrefix_FiltersOutTaggedFiles()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();

        var files = new List<string>
        {
            "file1.json.tracking",
            "file2.json.tracking",
            "file3.tags-Version=1.0.0.json.tracking",
            "file4.tags-Environment=Test.json.tracking"
        };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldContain("file2.json.tracking");
        result.AfterFilePaths.ShouldContain("file1.json.tracking");
        result.BeforeFilePaths.Count.ShouldBe(1);
        result.AfterFilePaths.Count.ShouldBe(1);
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithNoTagsAndFilesWithoutTagsPrefix_ReturnsFiles()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();

        var files = new List<string> { "file1.json.tracking", "file2.json.tracking", "file3.json.tracking" };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldContain("file2.json.tracking");
        result.AfterFilePaths.ShouldContain("file1.json.tracking");
        result.BeforeFilePaths.Count.ShouldBe(1);
        result.AfterFilePaths.Count.ShouldBe(1);
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithEmptyTags_TreatsAsNoTags()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary(); // Empty tags

        var files = new List<string>
        {
            "file1.json.tracking", "file2.json.tracking", "file3.tags-Version=1.0.0.json.tracking"
        };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldContain("file2.json.tracking");
        result.AfterFilePaths.ShouldContain("file1.json.tracking");
        result.BeforeFilePaths.Count.ShouldBe(1);
        result.AfterFilePaths.Count.ShouldBe(1);
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithSingleTagAndMatchingFiles_FiltersCorrectly()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();
        tags.Add("Version", "1.0.0");

        var files = new List<string>
        {
            "file1.tags-Version=1.0.0.json.tracking",
            "file2.tags-Version=1.0.0.json.tracking",
            "file3.tags-Version=2.0.0.json.tracking"
        };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldContain("file2.tags-Version=1.0.0.json.tracking");
        result.AfterFilePaths.ShouldContain("file1.tags-Version=1.0.0.json.tracking");
        result.BeforeFilePaths.Count.ShouldBe(1);
        result.AfterFilePaths.Count.ShouldBe(1);
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_WithTagsAndOnlyOneMatchingFile_ReturnsEmptyLists()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();
        tags.Add("Version", "1.0.0");

        var files = new List<string>
        {
            "file1.tags-Version=1.0.0.json.tracking",
            "file2.tags-Version=2.0.0.json.tracking",
            "file3.tags-Version=3.0.0.json.tracking"
        };

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns(files);

        // Act
        var result = _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeFilePaths.ShouldBeEmpty();
        result.AfterFilePaths.ShouldBeEmpty();
    }

    [Fact]
    public void GetBeforeAndAfterTrackingFiles_VerifyDirectoryParameterPassedToReader()
    {
        // Arrange
        var directory = Some.RandomString();
        var beforeTarget = Some.RandomString();
        var tags = new OrderedDictionary();

        _mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory)
            .Returns([]);

        // Act
        _trackingFileFinder.GetBeforeAndAfterTrackingFiles(directory, beforeTarget, tags);

        // Assert
        _mockTrackingFileDirectoryReader
            .Received(1)
            .FindTrackingFilesInDirectoryOrderedByLastModified(directory);
    }
}