using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.SailDiff;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.DefaultHandlers.SailDiff;

public class SailDiffGetLatestExecutionSummariesHandlerTests
{
    private readonly ITrackingFileDirectoryReader mockTrackingFileDirectoryReader;
    private readonly ITrackingFileParser mockTrackingFileParser;
    private readonly IRunSettings mockRunSettings;
    private readonly SailDiffGetLatestExecutionSummariesHandler handler;

    public SailDiffGetLatestExecutionSummariesHandlerTests()
    {
        mockTrackingFileDirectoryReader = Substitute.For<ITrackingFileDirectoryReader>();
        mockTrackingFileParser = Substitute.For<ITrackingFileParser>();
        mockRunSettings = Substitute.For<IRunSettings>();
        
        handler = new SailDiffGetLatestExecutionSummariesHandler(
            mockTrackingFileDirectoryReader,
            mockTrackingFileParser,
            mockRunSettings);
    }

    [Fact]
    public async Task Handle_WithValidTrackingFiles_ReturnsLatestExecutionSummary()
    {
        // Arrange
        var request = new GetLatestExecutionSummaryRequest();
        var trackingDirectory = Some.RandomString();
        var trackingFiles = new List<string> { "latest.json.tracking", "older.json.tracking" };
        var executionSummaries = CreateTrackingFileDataListWithData();

        mockRunSettings.GetRunSettingsTrackingDirectoryPath().Returns(trackingDirectory);
        mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(trackingDirectory)
            .Returns(trackingFiles);

        mockTrackingFileParser
            .TryParse(trackingFiles.First(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(executionSummaries);
                return true;
            });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.LatestExecutionSummaries.ShouldNotBeEmpty();
        result.LatestExecutionSummaries.Count.ShouldBe(1);
        
        // Verify it used the first (latest) tracking file
        await mockTrackingFileParser.Received(1)
            .TryParse(trackingFiles.First(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoTrackingFiles_ReturnsEmptyList()
    {
        // Arrange
        var request = new GetLatestExecutionSummaryRequest();
        var trackingDirectory = Some.RandomString();
        var emptyTrackingFiles = new List<string>();

        mockRunSettings.GetRunSettingsTrackingDirectoryPath().Returns(trackingDirectory);
        mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(trackingDirectory)
            .Returns(emptyTrackingFiles);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.LatestExecutionSummaries.ShouldBeEmpty();
        
        // Verify parser was not called since there are no files
        await mockTrackingFileParser.DidNotReceive()
            .TryParse(Arg.Any<string>(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithParsingFailure_ReturnsEmptyList()
    {
        // Arrange
        var request = new GetLatestExecutionSummaryRequest();
        var trackingDirectory = Some.RandomString();
        var trackingFiles = new List<string> { "corrupted.json.tracking" };

        mockRunSettings.GetRunSettingsTrackingDirectoryPath().Returns(trackingDirectory);
        mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(trackingDirectory)
            .Returns(trackingFiles);

        mockTrackingFileParser
            .TryParse(trackingFiles.First(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.LatestExecutionSummaries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidFile_ReturnsFirstExecutionSummary()
    {
        // Arrange
        var request = new GetLatestExecutionSummaryRequest();
        var trackingDirectory = Some.RandomString();
        var trackingFiles = new List<string> { "test.json.tracking" };
        
        var firstSummaryTracking = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(FirstTestClass))
            .Build();

        var secondSummaryTracking = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(SecondTestClass))
            .Build();

        var firstSummary = firstSummaryTracking.ToSummaryFormat();
        var secondSummary = secondSummaryTracking.ToSummaryFormat();

        var executionSummaries = new TrackingFileDataList();
        executionSummaries.Add(new List<IClassExecutionSummary> { firstSummary, secondSummary });

        mockRunSettings.GetRunSettingsTrackingDirectoryPath().Returns(trackingDirectory);
        mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(trackingDirectory)
            .Returns(trackingFiles);

        mockTrackingFileParser
            .TryParse(trackingFiles.First(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(executionSummaries);
                return true;
            });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.LatestExecutionSummaries.ShouldNotBeEmpty();
        result.LatestExecutionSummaries.Count.ShouldBe(2);
        
        // Verify it returns the first execution summary list
        result.LatestExecutionSummaries.First().TestClass.ShouldBe(typeof(FirstTestClass));
        result.LatestExecutionSummaries.Last().TestClass.ShouldBe(typeof(SecondTestClass));
    }

    [Fact]
    public async Task Handle_WithMultipleTrackingFiles_UsesOnlyTheFirst()
    {
        // Arrange
        var request = new GetLatestExecutionSummaryRequest();
        var trackingDirectory = Some.RandomString();
        var trackingFiles = new List<string> 
        { 
            "latest.json.tracking", 
            "older1.json.tracking", 
            "older2.json.tracking" 
        };
        var executionSummaries = CreateTrackingFileDataListWithData();

        mockRunSettings.GetRunSettingsTrackingDirectoryPath().Returns(trackingDirectory);
        mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(trackingDirectory)
            .Returns(trackingFiles);

        mockTrackingFileParser
            .TryParse(trackingFiles.First(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(executionSummaries);
                return true;
            });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.LatestExecutionSummaries.ShouldNotBeEmpty();
        
        // Verify only the first (latest) file was parsed
        await mockTrackingFileParser.Received(1)
            .TryParse(trackingFiles.First(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>());
        
        await mockTrackingFileParser.DidNotReceive()
            .TryParse(trackingFiles[1], Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>());
        
        await mockTrackingFileParser.DidNotReceive()
            .TryParse(trackingFiles[2], Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyParsedData_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new GetLatestExecutionSummaryRequest();
        var trackingDirectory = Some.RandomString();
        var trackingFiles = new List<string> { "empty.json.tracking" };

        mockRunSettings.GetRunSettingsTrackingDirectoryPath().Returns(trackingDirectory);
        mockTrackingFileDirectoryReader
            .FindTrackingFilesInDirectoryOrderedByLastModified(trackingDirectory)
            .Returns(trackingFiles);

        mockTrackingFileParser
            .TryParse(trackingFiles.First(), Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                // Parsing succeeds but returns empty data
                return true;
            });

        // Act & Assert
        // This test documents a bug in the handler - it should handle empty data gracefully
        // but currently throws InvalidOperationException when calling executionSummaries.First()
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await handler.Handle(request, CancellationToken.None));
    }

    private static TrackingFileDataList CreateTrackingFileDataListWithData()
    {
        var executionSummaryTracking = ClassExecutionSummaryTrackingFormatBuilder.Create().Build();
        var executionSummary = executionSummaryTracking.ToSummaryFormat();
        var dataList = new TrackingFileDataList();
        dataList.Add(new List<IClassExecutionSummary> { executionSummary });
        return dataList;
    }

    // Test classes for verification
    private class FirstTestClass { }
    private class SecondTestClass { }
}
