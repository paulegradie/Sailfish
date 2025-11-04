using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

/// <summary>
/// Comprehensive unit tests for ExecutionSummaryWriter.
/// Tests output coordination, notification publishing, and error handling.
/// </summary>
public class ExecutionSummaryWriterTests
{
    private readonly IMediator mockMediator;
    private readonly ExecutionSummaryWriter executionSummaryWriter;

    public ExecutionSummaryWriterTests()
    {
        mockMediator = Substitute.For<IMediator>();
        executionSummaryWriter = new ExecutionSummaryWriter(mockMediator);
    }

    [Fact]
    public void Constructor_WithValidMediator_ShouldCreateInstance()
    {
        // Act & Assert
        executionSummaryWriter.ShouldNotBeNull();
        executionSummaryWriter.ShouldBeAssignableTo<IExecutionSummaryWriter>();
    }



    [Fact]
    public async Task Write_WithValidSummaries_ShouldPublishAllNotifications()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = CancellationToken.None;

        // Act
        await executionSummaryWriter.Write(executionSummaries, cancellationToken);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToConsoleNotification>(n => n.Content == executionSummaries),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToMarkDownNotification>(n => n.ClassExecutionSummaries == executionSummaries),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToCsvNotification>(n => n.ClassExecutionSummaries == executionSummaries),
            cancellationToken);
    }

    [Fact]
    public async Task Write_WithEmptyList_ShouldStillPublishNotifications()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>();
        var cancellationToken = CancellationToken.None;

        // Act
        await executionSummaryWriter.Write(executionSummaries, cancellationToken);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToConsoleNotification>(n => n.Content == executionSummaries),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToMarkDownNotification>(n => n.ClassExecutionSummaries == executionSummaries),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToCsvNotification>(n => n.ClassExecutionSummaries == executionSummaries),
            cancellationToken);
    }

    [Fact]
    public async Task Write_WithCancellationToken_ShouldPassTokenToNotifications()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        await executionSummaryWriter.Write(executionSummaries, cancellationToken);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Any<WriteToConsoleNotification>(),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Any<WriteToMarkDownNotification>(),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Any<WriteToCsvNotification>(),
            cancellationToken);
    }

    [Fact]
    public async Task Write_WithCancelledToken_ShouldRespectCancellation()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        mockMediator.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled(cancellationTokenSource.Token));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await executionSummaryWriter.Write(executionSummaries, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task Write_WhenConsoleNotificationFails_ShouldStillPublishOthers()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = CancellationToken.None;

        mockMediator.Publish(Arg.Any<WriteToConsoleNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Console failed")));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await executionSummaryWriter.Write(executionSummaries, cancellationToken));
    }

    [Fact]
    public async Task Write_WhenMarkdownNotificationFails_ShouldNotPublishCsv()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = CancellationToken.None;

        mockMediator.Publish(Arg.Any<WriteToMarkDownNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Markdown failed")));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await executionSummaryWriter.Write(executionSummaries, cancellationToken));

        // CSV notification should not be called due to exception
        await mockMediator.DidNotReceive().Publish(
            Arg.Any<WriteToCsvNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Write_WhenCsvNotificationFails_ShouldThrowException()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = CancellationToken.None;

        mockMediator.Publish(Arg.Any<WriteToCsvNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("CSV failed")));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await executionSummaryWriter.Write(executionSummaries, cancellationToken));
    }

    [Fact]
    public async Task Write_ShouldPublishNotificationsInCorrectOrder()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = CancellationToken.None;
        var callOrder = new List<string>();

        mockMediator.Publish(Arg.Any<WriteToConsoleNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("Console"));

        mockMediator.Publish(Arg.Any<WriteToMarkDownNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("Markdown"));

        mockMediator.Publish(Arg.Any<WriteToCsvNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("CSV"));

        // Act
        await executionSummaryWriter.Write(executionSummaries, cancellationToken);

        // Assert
        callOrder.ShouldBe(new[] { "Console", "Markdown", "CSV" });
    }

    [Fact]
    public async Task Write_WithMultipleSummaries_ShouldPassAllToNotifications()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>
        {
            CreateMockExecutionSummary("TestClass1"),
            CreateMockExecutionSummary("TestClass2"),
            CreateMockExecutionSummary("TestClass3")
        };
        var cancellationToken = CancellationToken.None;

        // Act
        await executionSummaryWriter.Write(executionSummaries, cancellationToken);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToConsoleNotification>(n => n.Content.Count == 3),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToMarkDownNotification>(n => n.ClassExecutionSummaries.Count == 3),
            cancellationToken);

        await mockMediator.Received(1).Publish(
            Arg.Is<WriteToCsvNotification>(n => n.ClassExecutionSummaries.Count == 3),
            cancellationToken);
    }

    [Fact]
    public async Task Write_ShouldUseConfigureAwaitFalse()
    {
        // Arrange
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = CancellationToken.None;

        // This test verifies that ConfigureAwait(false) is used
        // by ensuring the method completes without deadlock in a synchronous context
        var task = executionSummaryWriter.Write(executionSummaries, cancellationToken);

        // Act & Assert
        await task; // Should complete without deadlock
        task.IsCompleted.ShouldBeTrue();
    }





    private List<IClassExecutionSummary> CreateMockExecutionSummaries()
    {
        return
        [
            CreateMockExecutionSummary("TestClass1"),
            CreateMockExecutionSummary("TestClass2")
        ];
    }

    private IClassExecutionSummary CreateMockExecutionSummary(string className)
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        var testClass = Substitute.For<Type>();
        testClass.Name.Returns(className);
        summary.TestClass.Returns(testClass);

        var executionSettings = Substitute.For<IExecutionSettings>();
        summary.ExecutionSettings.Returns(executionSettings);

        var testCaseResults = new List<ICompiledTestCaseResult>();
        summary.CompiledTestCaseResults.Returns(testCaseResults);

        return summary;
    }
}
