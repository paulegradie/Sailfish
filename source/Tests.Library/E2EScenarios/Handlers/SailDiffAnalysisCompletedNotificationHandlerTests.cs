using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.DefaultHandlers.SailDiff;
using Sailfish.Exceptions;
using Sailfish.Presentation;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.E2EScenarios.Handlers;

/// <summary>
/// Unit tests for SailDiffAnalysisCompleteNotificationHandler to ensure proper
/// markdown and CSV file writing functionality with comprehensive error handling.
/// </summary>
public class SailDiffAnalysisCompleteNotificationHandlerTests : IDisposable
{
    private readonly List<string> _tempDirectories;

    public SailDiffAnalysisCompleteNotificationHandlerTests()
    {
        _tempDirectories = [];
    }

    [Fact]
    public async Task HandlerHandles()
    {
        var outputDirectory = CreateTempDirectory();
        var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build();
        var handler = new SailDiffAnalysisCompleteNotificationHandler(settings);

        var testCaseId = Some.SimpleTestCaseId();

        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(new StatisticalTestResult(
            5.0,
            6.0,
            5.0,
            5.0,
            345,
            0.001,
            SailfishChangeDirection.NoChange,
            3,
            3,
            [1.0, 2, 3],
            [9.0, 10, 11],
            new Dictionary<string, object>()), null, null);


        SailDiffResult[] results = [new SailDiffResult(testCaseId, testResultWithOutlierAnalysis)];

        var notification = new SailDiffAnalysisCompleteNotification(results, "This is some markdown");

        await handler.Handle(notification, CancellationToken.None);

        var files = Directory.GetFiles(outputDirectory);
        var mdContent = await File.ReadAllTextAsync(files.Single(x => x.EndsWith(DefaultFileSettings.MarkdownSuffix)));
        var csvContent = await File.ReadAllTextAsync(files.Single(x => x.EndsWith(DefaultFileSettings.CsvSuffix)));
        const string expectedCsv = """
                                   5,6,5,5,345,0.001,No Change,3,3,"1,2,3","9,10,11"
                                   """;
        files.Length.ShouldBe(2);
        mdContent.ShouldBe("This is some markdown");
        csvContent.Trim().ShouldEndWith(expectedCsv);
    }

    [Fact]
    public async Task Handle_WithEmptyMarkdown_ShouldNotCreateMarkdownFile()
    {
        // Arrange
        var outputDirectory = CreateTempDirectory();
        var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build();
        var handler = new SailDiffAnalysisCompleteNotificationHandler(settings);

        var testCaseId = Some.SimpleTestCaseId();
        var testResult = CreateTestResultWithOutlierAnalysis();
        SailDiffResult[] results = [new SailDiffResult(testCaseId, testResult)];

        var notification = new SailDiffAnalysisCompleteNotification(results, string.Empty);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        var files = Directory.GetFiles(outputDirectory);
        files.Where(f => f.EndsWith(DefaultFileSettings.MarkdownSuffix)).ShouldBeEmpty();
        files.Where(f => f.EndsWith(DefaultFileSettings.CsvSuffix)).ShouldNotBeEmpty(); // CSV should still be created
    }

    [Fact]
    public async Task Handle_WithNullMarkdown_ShouldNotCreateMarkdownFile()
    {
        // Arrange
        var outputDirectory = CreateTempDirectory();
        var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build();
        var handler = new SailDiffAnalysisCompleteNotificationHandler(settings);

        var testCaseId = Some.SimpleTestCaseId();
        var testResult = CreateTestResultWithOutlierAnalysis();
        SailDiffResult[] results = [new SailDiffResult(testCaseId, testResult)];

        var notification = new SailDiffAnalysisCompleteNotification(results, null!);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        var files = Directory.GetFiles(outputDirectory);
        files.Where(f => f.EndsWith(DefaultFileSettings.MarkdownSuffix)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ShouldNotCreateCsvFile()
    {
        // Arrange
        var outputDirectory = CreateTempDirectory();
        var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build();
        var handler = new SailDiffAnalysisCompleteNotificationHandler(settings);

        var notification = new SailDiffAnalysisCompleteNotification(new SailDiffResult[0], "Some markdown");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        var files = Directory.GetFiles(outputDirectory);
        files.Where(f => f.EndsWith(DefaultFileSettings.CsvSuffix)).ShouldBeEmpty();
        files.Where(f => f.EndsWith(DefaultFileSettings.MarkdownSuffix)).ShouldNotBeEmpty(); // Markdown should still be created
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var outputDirectory = CreateTempDirectory();
        var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build();
        var handler = new SailDiffAnalysisCompleteNotificationHandler(settings);

        var testCaseId = Some.SimpleTestCaseId();
        var testResult = CreateTestResultWithOutlierAnalysis();
        SailDiffResult[] results = [new SailDiffResult(testCaseId, testResult)];

        var notification = new SailDiffAnalysisCompleteNotification(results, "Test markdown");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await handler.Handle(notification, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task Handle_WithDirectoryAsFilePath_ShouldThrowIOException()
    {
        // Arrange
        var outputDirectory = CreateTempDirectory();
        var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build();
        var handler = new SailDiffAnalysisCompleteNotificationHandler(settings);

        // Create a directory with the same name as the expected markdown file
        var expectedFileName = DefaultFileSettings.AppendTagsToFilename(
            DefaultFileSettings.DefaultSaildiffMarkdownFileName(settings.TimeStamp, settings.SailDiffSettings.TestType),
            settings.Tags);
        var conflictingDirectory = Path.Join(outputDirectory, expectedFileName);
        Directory.CreateDirectory(conflictingDirectory);

        var testCaseId = Some.SimpleTestCaseId();
        var testResult = CreateTestResultWithOutlierAnalysis();
        SailDiffResult[] results = [new SailDiffResult(testCaseId, testResult)];

        var notification = new SailDiffAnalysisCompleteNotification(results, "Test markdown");

        // Act & Assert
        await Should.ThrowAsync<IOException>(async () =>
            await handler.Handle(notification, CancellationToken.None));
    }

    private string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sailfish_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    private TestResultWithOutlierAnalysis CreateTestResultWithOutlierAnalysis()
    {
        return new TestResultWithOutlierAnalysis(new StatisticalTestResult(
            10.0, 12.0, 10.0, 10.0, 100, 0.05,
            SailfishChangeDirection.Improved, 5, 5,
            [8.0, 9.0, 10.0, 11.0, 12.0],
            [18.0, 19.0, 20.0, 21.0, 22.0],
            new Dictionary<string, object>()), null, null);
    }

    public void Dispose()
    {
        // Cleanup temp directories
        foreach (var tempDir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    // Remove read-only attributes from files
                    var files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                    }
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}