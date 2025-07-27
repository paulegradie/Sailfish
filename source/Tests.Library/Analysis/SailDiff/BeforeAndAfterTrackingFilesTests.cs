using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class BeforeAndAfterTrackingFilesTests
{
    [Fact]
    public void Constructor_WithValidLists_SetsPropertiesCorrectly()
    {
        // Arrange
        var beforeFiles = new List<string> { Some.RandomString(), Some.RandomString() };
        var afterFiles = new List<string> { Some.RandomString(), Some.RandomString() };

        // Act
        var result = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles);

        // Assert
        result.BeforeFilePaths.ShouldBe(beforeFiles);
        result.AfterFilePaths.ShouldBe(afterFiles);
    }

    [Fact]
    public void Constructor_WithEmptyLists_SetsPropertiesCorrectly()
    {
        // Arrange
        var beforeFiles = new List<string>();
        var afterFiles = new List<string>();

        // Act
        var result = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles);

        // Assert
        result.BeforeFilePaths.ShouldBe(beforeFiles);
        result.AfterFilePaths.ShouldBe(afterFiles);
        result.BeforeFilePaths.ShouldBeEmpty();
        result.AfterFilePaths.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullBeforeList_SetsPropertyToNull()
    {
        // Arrange
        List<string>? beforeFiles = null;
        var afterFiles = new List<string> { Some.RandomString() };

        // Act
        var result = new BeforeAndAfterTrackingFiles(beforeFiles!, afterFiles);

        // Assert
        result.BeforeFilePaths.ShouldBeNull();
        result.AfterFilePaths.ShouldBe(afterFiles);
    }

    [Fact]
    public void Constructor_WithNullAfterList_SetsPropertyToNull()
    {
        // Arrange
        var beforeFiles = new List<string> { Some.RandomString() };
        List<string>? afterFiles = null;

        // Act
        var result = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles!);

        // Assert
        result.BeforeFilePaths.ShouldBe(beforeFiles);
        result.AfterFilePaths.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithBothNullLists_SetsPropertiesToNull()
    {
        // Arrange
        List<string>? beforeFiles = null;
        List<string>? afterFiles = null;

        // Act
        var result = new BeforeAndAfterTrackingFiles(beforeFiles!, afterFiles!);

        // Assert
        result.BeforeFilePaths.ShouldBeNull();
        result.AfterFilePaths.ShouldBeNull();
    }

    [Fact]
    public void BeforeFilePaths_Get_ReturnsConstructorValue()
    {
        // Arrange
        var beforeFiles = new List<string> { Some.RandomString(), Some.RandomString() };
        var afterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles);

        // Act
        var result = trackingFiles.BeforeFilePaths;

        // Assert
        result.ShouldBe(beforeFiles);
        result.ShouldBeSameAs(beforeFiles);
    }

    [Fact]
    public void AfterFilePaths_Get_ReturnsConstructorValue()
    {
        // Arrange
        var beforeFiles = new List<string> { Some.RandomString() };
        var afterFiles = new List<string> { Some.RandomString(), Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles);

        // Act
        var result = trackingFiles.AfterFilePaths;

        // Assert
        result.ShouldBe(afterFiles);
        result.ShouldBeSameAs(afterFiles);
    }

    [Fact]
    public void BeforeFilePaths_Set_UpdatesProperty()
    {
        // Arrange
        var initialBeforeFiles = new List<string> { Some.RandomString() };
        var initialAfterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(initialBeforeFiles, initialAfterFiles);
        var newBeforeFiles = new List<string> { Some.RandomString(), Some.RandomString() };

        // Act
        trackingFiles.BeforeFilePaths = newBeforeFiles;

        // Assert
        trackingFiles.BeforeFilePaths.ShouldBe(newBeforeFiles);
        trackingFiles.BeforeFilePaths.ShouldBeSameAs(newBeforeFiles);
        trackingFiles.AfterFilePaths.ShouldBe(initialAfterFiles);
    }

    [Fact]
    public void AfterFilePaths_Set_UpdatesProperty()
    {
        // Arrange
        var initialBeforeFiles = new List<string> { Some.RandomString() };
        var initialAfterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(initialBeforeFiles, initialAfterFiles);
        var newAfterFiles = new List<string> { Some.RandomString(), Some.RandomString() };

        // Act
        trackingFiles.AfterFilePaths = newAfterFiles;

        // Assert
        trackingFiles.AfterFilePaths.ShouldBe(newAfterFiles);
        trackingFiles.AfterFilePaths.ShouldBeSameAs(newAfterFiles);
        trackingFiles.BeforeFilePaths.ShouldBe(initialBeforeFiles);
    }

    [Fact]
    public void BeforeFilePaths_SetToNull_UpdatesProperty()
    {
        // Arrange
        var initialBeforeFiles = new List<string> { Some.RandomString() };
        var initialAfterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(initialBeforeFiles, initialAfterFiles);

        // Act
        trackingFiles.BeforeFilePaths = null!;

        // Assert
        trackingFiles.BeforeFilePaths.ShouldBeNull();
        trackingFiles.AfterFilePaths.ShouldBe(initialAfterFiles);
    }

    [Fact]
    public void AfterFilePaths_SetToNull_UpdatesProperty()
    {
        // Arrange
        var initialBeforeFiles = new List<string> { Some.RandomString() };
        var initialAfterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(initialBeforeFiles, initialAfterFiles);

        // Act
        trackingFiles.AfterFilePaths = null!;

        // Assert
        trackingFiles.AfterFilePaths.ShouldBeNull();
        trackingFiles.BeforeFilePaths.ShouldBe(initialBeforeFiles);
    }

    [Fact]
    public void BeforeFilePaths_SetToEmptyList_UpdatesProperty()
    {
        // Arrange
        var initialBeforeFiles = new List<string> { Some.RandomString() };
        var initialAfterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(initialBeforeFiles, initialAfterFiles);
        var emptyList = new List<string>();

        // Act
        trackingFiles.BeforeFilePaths = emptyList;

        // Assert
        trackingFiles.BeforeFilePaths.ShouldBe(emptyList);
        trackingFiles.BeforeFilePaths.ShouldBeEmpty();
        trackingFiles.AfterFilePaths.ShouldBe(initialAfterFiles);
    }

    [Fact]
    public void AfterFilePaths_SetToEmptyList_UpdatesProperty()
    {
        // Arrange
        var initialBeforeFiles = new List<string> { Some.RandomString() };
        var initialAfterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(initialBeforeFiles, initialAfterFiles);
        var emptyList = new List<string>();

        // Act
        trackingFiles.AfterFilePaths = emptyList;

        // Assert
        trackingFiles.AfterFilePaths.ShouldBe(emptyList);
        trackingFiles.AfterFilePaths.ShouldBeEmpty();
        trackingFiles.BeforeFilePaths.ShouldBe(initialBeforeFiles);
    }

    [Fact]
    public void Properties_AfterConstruction_AreIndependentOfOriginalLists()
    {
        // Arrange
        var beforeFiles = new List<string> { Some.RandomString() };
        var afterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles);

        // Act
        beforeFiles.Add(Some.RandomString());
        afterFiles.Add(Some.RandomString());

        // Assert
        trackingFiles.BeforeFilePaths.Count.ShouldBe(2); // Original + added
        trackingFiles.AfterFilePaths.Count.ShouldBe(2); // Original + added
        trackingFiles.BeforeFilePaths.ShouldBeSameAs(beforeFiles);
        trackingFiles.AfterFilePaths.ShouldBeSameAs(afterFiles);
    }

    [Fact]
    public void Constructor_WithListsContainingNullElements_HandlesCorrectly()
    {
        // Arrange
        var beforeFiles = new List<string> { Some.RandomString(), null!, Some.RandomString() };
        var afterFiles = new List<string> { null!, Some.RandomString() };

        // Act
        var result = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles);

        // Assert
        result.BeforeFilePaths.ShouldBe(beforeFiles);
        result.AfterFilePaths.ShouldBe(afterFiles);
        result.BeforeFilePaths.Count.ShouldBe(3);
        result.AfterFilePaths.Count.ShouldBe(2);
        result.BeforeFilePaths[1].ShouldBeNull();
        result.AfterFilePaths[0].ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithLargeListsOfFiles_HandlesCorrectly()
    {
        // Arrange
        var beforeFiles = new List<string>();
        var afterFiles = new List<string>();
        
        for (int i = 0; i < 1000; i++)
        {
            beforeFiles.Add($"before_file_{i}.json");
            afterFiles.Add($"after_file_{i}.json");
        }

        // Act
        var result = new BeforeAndAfterTrackingFiles(beforeFiles, afterFiles);

        // Assert
        result.BeforeFilePaths.ShouldBe(beforeFiles);
        result.AfterFilePaths.ShouldBe(afterFiles);
        result.BeforeFilePaths.Count.ShouldBe(1000);
        result.AfterFilePaths.Count.ShouldBe(1000);
    }

    [Fact]
    public void Properties_CanBeSetMultipleTimes_UpdatesCorrectly()
    {
        // Arrange
        var initialBeforeFiles = new List<string> { Some.RandomString() };
        var initialAfterFiles = new List<string> { Some.RandomString() };
        var trackingFiles = new BeforeAndAfterTrackingFiles(initialBeforeFiles, initialAfterFiles);

        var firstUpdate = new List<string> { Some.RandomString(), Some.RandomString() };
        var secondUpdate = new List<string> { Some.RandomString() };

        // Act & Assert - First update
        trackingFiles.BeforeFilePaths = firstUpdate;
        trackingFiles.BeforeFilePaths.ShouldBe(firstUpdate);
        trackingFiles.BeforeFilePaths.Count.ShouldBe(2);

        // Act & Assert - Second update
        trackingFiles.BeforeFilePaths = secondUpdate;
        trackingFiles.BeforeFilePaths.ShouldBe(secondUpdate);
        trackingFiles.BeforeFilePaths.Count.ShouldBe(1);
    }
}
