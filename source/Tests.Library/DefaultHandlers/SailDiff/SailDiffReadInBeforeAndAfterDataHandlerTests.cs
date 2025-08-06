using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.SailDiff;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.DefaultHandlers.SailDiff;

public class SailDiffReadInBeforeAndAfterDataHandlerTests
{
    private readonly ITrackingFileParser mockTrackingFileParser;
    private readonly ILogger mockLogger;
    private readonly SailDiffReadInBeforeAndAfterDataHandler handler;

    public SailDiffReadInBeforeAndAfterDataHandlerTests()
    {
        mockTrackingFileParser = Substitute.For<ITrackingFileParser>();
        mockLogger = Substitute.For<ILogger>();
        handler = new SailDiffReadInBeforeAndAfterDataHandler(mockTrackingFileParser, mockLogger);
    }

    [Fact]
    public async Task Handle_WithValidFiles_ReturnsSuccessfulResponse()
    {
        // Arrange
        var beforeFilePaths = new[] { "before1.json", "before2.json" };
        var afterFilePaths = new[] { "after1.json", "after2.json" };
        var request = new ReadInBeforeAndAfterDataRequest(beforeFilePaths, afterFilePaths);

        var beforeData = CreateTrackingFileDataListWithData();
        var afterData = CreateTrackingFileDataListWithData();

        mockTrackingFileParser
            .TryParseMany(beforeFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(beforeData);
                return true;
            });

        mockTrackingFileParser
            .TryParseMany(afterFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(afterData);
                return true;
            });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeData.ShouldNotBeNull();
        result.AfterData.ShouldNotBeNull();
        
        result.BeforeData.TestIds.ShouldBe(beforeFilePaths);
        result.AfterData.TestIds.ShouldBe(afterFilePaths);
        
        result.BeforeData.Data.ShouldNotBeEmpty();
        result.AfterData.Data.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithBeforeParsingFailure_ReturnsNullResponse()
    {
        // Arrange
        var beforeFilePaths = new[] { "before.json" };
        var afterFilePaths = new[] { "after.json" };
        var request = new ReadInBeforeAndAfterDataRequest(beforeFilePaths, afterFilePaths);

        mockTrackingFileParser
            .TryParseMany(beforeFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeData.ShouldBeNull();
        result.AfterData.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithAfterParsingFailure_ReturnsNullResponse()
    {
        // Arrange
        var beforeFilePaths = new[] { "before.json" };
        var afterFilePaths = new[] { "after.json" };
        var request = new ReadInBeforeAndAfterDataRequest(beforeFilePaths, afterFilePaths);

        var beforeData = CreateTrackingFileDataListWithData();

        mockTrackingFileParser
            .TryParseMany(beforeFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(beforeData);
                return true;
            });

        mockTrackingFileParser
            .TryParseMany(afterFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeData.ShouldBeNull();
        result.AfterData.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyBeforeData_ReturnsNullResponse()
    {
        // Arrange
        var beforeFilePaths = new[] { "before.json" };
        var afterFilePaths = new[] { "after.json" };
        var request = new ReadInBeforeAndAfterDataRequest(beforeFilePaths, afterFilePaths);

        var afterData = CreateTrackingFileDataListWithData();

        mockTrackingFileParser
            .TryParseMany(beforeFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(true); // Parsing succeeds but data is empty

        mockTrackingFileParser
            .TryParseMany(afterFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(afterData);
                return true;
            });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeData.ShouldBeNull();
        result.AfterData.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyAfterData_ReturnsNullResponse()
    {
        // Arrange
        var beforeFilePaths = new[] { "before.json" };
        var afterFilePaths = new[] { "after.json" };
        var request = new ReadInBeforeAndAfterDataRequest(beforeFilePaths, afterFilePaths);

        var beforeData = CreateTrackingFileDataListWithData();

        mockTrackingFileParser
            .TryParseMany(beforeFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(beforeData);
                return true;
            });

        mockTrackingFileParser
            .TryParseMany(afterFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(true); // Parsing succeeds but data is empty

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeData.ShouldBeNull();
        result.AfterData.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithValidData_ExtractsPerformanceResults()
    {
        // Arrange
        var beforeFilePaths = new[] { "before.json" };
        var afterFilePaths = new[] { "after.json" };
        var request = new ReadInBeforeAndAfterDataRequest(beforeFilePaths, afterFilePaths);

        var beforeData = CreateTrackingFileDataListWithSpecificData("BeforeTest");
        var afterData = CreateTrackingFileDataListWithSpecificData("AfterTest");

        mockTrackingFileParser
            .TryParseMany(beforeFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(beforeData);
                return true;
            });

        mockTrackingFileParser
            .TryParseMany(afterFilePaths, Arg.Any<TrackingFileDataList>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dataList = callInfo.Arg<TrackingFileDataList>();
                dataList.AddRange(afterData);
                return true;
            });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.BeforeData.ShouldNotBeNull();
        result.AfterData.ShouldNotBeNull();

        var beforePerformanceResult = result.BeforeData.Data.First();
        var afterPerformanceResult = result.AfterData.Data.First();

        beforePerformanceResult.DisplayName.ShouldBe("BeforeTest");
        afterPerformanceResult.DisplayName.ShouldBe("AfterTest");
    }

    private static TrackingFileDataList CreateTrackingFileDataListWithData()
    {
        return CreateTrackingFileDataListWithSpecificData(Some.RandomString());
    }

    private static TrackingFileDataList CreateTrackingFileDataListWithSpecificData(string displayName)
    {
        var performanceResult = PerformanceRunResultTrackingFormatBuilder.Create()
            .WithDisplayName(displayName)
            .Build();

        var compiledTestCase = CompiledTestCaseResultTrackingFormatBuilder.Create()
            .WithPerformanceRunResult(performanceResult)
            .Build();

        var executionSummaryTracking = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithCompiledTestCaseResult(new List<CompiledTestCaseResultTrackingFormat> { compiledTestCase })
            .Build();

        var executionSummary = executionSummaryTracking.ToSummaryFormat();

        var dataList = new TrackingFileDataList();
        dataList.Add(new List<IClassExecutionSummary> { executionSummary });
        return dataList;
    }
}
