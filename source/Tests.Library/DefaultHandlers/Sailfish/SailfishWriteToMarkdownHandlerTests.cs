using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish; // RunSettingsBuilder
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Presentation.Markdown;
using Shouldly;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class SailfishWriteToMarkdownHandlerTests
{
    [Fact]
    public async Task Handle_CreatesOutputDirectory_And_CallsWriteEnhanced_WithExpectedFilePath()
    {
        // Arrange
        var writer = Substitute.For<IMarkdownWriter>();
        var fixedUtc = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc);
        var tempDir = Path.Combine(Path.GetTempPath(), "Sailfish_Md_" + Guid.NewGuid().ToString("N"), "out");
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);

        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTimeStamp(fixedUtc)
            .WithLocalOutputDirectory(tempDir)
            .WithTag("env", "local")
            .WithTag("build", "123")
            .Build();

        var handler = new SailfishWriteToMarkdownHandler(writer, runSettings);

        var summaries = new List<IClassExecutionSummary>
        {
            CreateStubSummary()
        };
        var notification = new WriteToMarkDownNotification(summaries);

        var expectedFileName = DefaultFileSettings.AppendTagsToFilename(
            DefaultFileSettings.DefaultPerformanceResultsFileNameStem(runSettings.TimeStamp) + ".md",
            runSettings.Tags);
        var expectedPath = Path.Combine(runSettings.LocalOutputDirectory, expectedFileName);

        try
        {
            // Act
            await handler.Handle(notification, CancellationToken.None);

            // Assert: directory created
            Directory.Exists(tempDir).ShouldBeTrue();

            // Assert: enhanced writer called, legacy not called
            await writer.Received(1).WriteEnhanced(
                Arg.Is<IEnumerable<IClassExecutionSummary>>(xs => ReferenceEquals(xs, summaries)),
                Arg.Is<string>(p => p == expectedPath),
                Arg.Any<CancellationToken>());

            await writer.DidNotReceive().Write(Arg.Any<IEnumerable<IClassExecutionSummary>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Handle_WhenEnhancedNotImplemented_FallsBackToLegacyWrite()
    {
        // Arrange
        var writer = Substitute.For<IMarkdownWriter>();
        writer
            .WriteEnhanced(Arg.Any<IEnumerable<IClassExecutionSummary>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromException(new NotImplementedException()));

        var fixedUtc = new DateTime(2024, 06, 07, 08, 09, 10, DateTimeKind.Utc);
        var tempDir = Path.Combine(Path.GetTempPath(), "Sailfish_Md_" + Guid.NewGuid().ToString("N"));
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);

        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTimeStamp(fixedUtc)
            .WithLocalOutputDirectory(tempDir)
            .Build();

        var handler = new SailfishWriteToMarkdownHandler(writer, runSettings);

        var summaries = new List<IClassExecutionSummary>();
        var notification = new WriteToMarkDownNotification(summaries);

        var expectedFileName = DefaultFileSettings.AppendTagsToFilename(
            DefaultFileSettings.DefaultPerformanceResultsFileNameStem(runSettings.TimeStamp) + ".md",
            runSettings.Tags);
        var expectedPath = Path.Combine(runSettings.LocalOutputDirectory, expectedFileName);

        try
        {
            // Act
            await handler.Handle(notification, CancellationToken.None);

            // Assert: directory created
            Directory.Exists(tempDir).ShouldBeTrue();

            // Enhanced attempted
            await writer.Received(1).WriteEnhanced(Arg.Any<IEnumerable<IClassExecutionSummary>>(), Arg.Is<string>(s => s == expectedPath), Arg.Any<CancellationToken>());
            // Fallback used
            await writer.Received(1).Write(
                Arg.Is<IEnumerable<IClassExecutionSummary>>(xs => ReferenceEquals(xs, summaries)),
                Arg.Is<string>(p => p == expectedPath),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    private static IClassExecutionSummary CreateStubSummary()
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(object));
        summary.ExecutionSettings.Returns(new ExecutionSettings());
        summary.CompiledTestCaseResults.Returns(Array.Empty<ICompiledTestCaseResult>());
        summary.GetSuccessfulTestCases().Returns(Array.Empty<ICompiledTestCaseResult>());
        summary.GetFailedTestCases().Returns(Array.Empty<ICompiledTestCaseResult>());
        summary.FilterForFailureTestCases().Returns(summary);
        summary.FilterForSuccessfulTestCases().Returns(summary);
        return summary;
    }
}

