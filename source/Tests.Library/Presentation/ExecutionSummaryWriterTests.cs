using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

/// <summary>
/// Unit tests for ExecutionSummaryWriter. It now orchestrates three direct writer collaborators
/// (console, markdown, csv) instead of publishing three internal notifications through the mediator —
/// so these tests verify the ordered, sequential delegation and fail-fast propagation.
/// </summary>
public class ExecutionSummaryWriterTests
{
    private readonly IConsoleSummaryWriter _consoleSummaryWriter;
    private readonly ICsvSummaryWriter _csvSummaryWriter;
    private readonly ExecutionSummaryWriter _executionSummaryWriter;
    private readonly IMarkdownSummaryWriter _markdownSummaryWriter;

    public ExecutionSummaryWriterTests()
    {
        _consoleSummaryWriter = Substitute.For<IConsoleSummaryWriter>();
        _markdownSummaryWriter = Substitute.For<IMarkdownSummaryWriter>();
        _csvSummaryWriter = Substitute.For<ICsvSummaryWriter>();
        _executionSummaryWriter = new ExecutionSummaryWriter(_consoleSummaryWriter, _markdownSummaryWriter, _csvSummaryWriter);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        _executionSummaryWriter.ShouldNotBeNull();
        _executionSummaryWriter.ShouldBeAssignableTo<IExecutionSummaryWriter>();
    }

    [Fact]
    public async Task Write_WithValidSummaries_ShouldInvokeAllWriters()
    {
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = CancellationToken.None;

        await _executionSummaryWriter.Write(executionSummaries, cancellationToken);

        await _consoleSummaryWriter.Received(1).Write(executionSummaries, cancellationToken);
        await _markdownSummaryWriter.Received(1).Write(executionSummaries, cancellationToken);
        await _csvSummaryWriter.Received(1).Write(executionSummaries, cancellationToken);
    }

    [Fact]
    public async Task Write_WithEmptyList_ShouldStillInvokeAllWriters()
    {
        var executionSummaries = new List<IClassExecutionSummary>();
        var cancellationToken = CancellationToken.None;

        await _executionSummaryWriter.Write(executionSummaries, cancellationToken);

        await _consoleSummaryWriter.Received(1).Write(executionSummaries, cancellationToken);
        await _markdownSummaryWriter.Received(1).Write(executionSummaries, cancellationToken);
        await _csvSummaryWriter.Received(1).Write(executionSummaries, cancellationToken);
    }

    [Fact]
    public async Task Write_WithCancellationToken_ShouldPassTokenToWriters()
    {
        var executionSummaries = CreateMockExecutionSummaries();
        var cancellationToken = new CancellationTokenSource().Token;

        await _executionSummaryWriter.Write(executionSummaries, cancellationToken);

        await _consoleSummaryWriter.Received(1).Write(Arg.Any<List<IClassExecutionSummary>>(), cancellationToken);
        await _markdownSummaryWriter.Received(1).Write(Arg.Any<List<IClassExecutionSummary>>(), cancellationToken);
        await _csvSummaryWriter.Received(1).Write(Arg.Any<List<IClassExecutionSummary>>(), cancellationToken);
    }

    [Fact]
    public async Task Write_ShouldInvokeWritersInOrder_ConsoleThenMarkdownThenCsv()
    {
        var executionSummaries = CreateMockExecutionSummaries();
        var callOrder = new List<string>();

        _consoleSummaryWriter.Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask).AndDoes(_ => callOrder.Add("Console"));
        _markdownSummaryWriter.Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask).AndDoes(_ => callOrder.Add("Markdown"));
        _csvSummaryWriter.Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask).AndDoes(_ => callOrder.Add("CSV"));

        await _executionSummaryWriter.Write(executionSummaries, CancellationToken.None);

        callOrder.ShouldBe(new[] { "Console", "Markdown", "CSV" });
    }

    [Fact]
    public async Task Write_WhenConsoleWriterThrows_ShouldNotInvokeOthers()
    {
        var executionSummaries = CreateMockExecutionSummaries();
        _consoleSummaryWriter.Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Console failed")));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _executionSummaryWriter.Write(executionSummaries, CancellationToken.None));

        await _markdownSummaryWriter.DidNotReceive().Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>());
        await _csvSummaryWriter.DidNotReceive().Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Write_WhenMarkdownWriterThrows_ShouldNotInvokeCsv()
    {
        var executionSummaries = CreateMockExecutionSummaries();
        _markdownSummaryWriter.Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Markdown failed")));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _executionSummaryWriter.Write(executionSummaries, CancellationToken.None));

        await _csvSummaryWriter.DidNotReceive().Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Write_WhenCsvWriterThrows_ShouldPropagate()
    {
        var executionSummaries = CreateMockExecutionSummaries();
        _csvSummaryWriter.Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("CSV failed")));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _executionSummaryWriter.Write(executionSummaries, CancellationToken.None));
    }

    [Fact]
    public async Task Write_WithCancelledToken_ShouldRespectCancellation()
    {
        var executionSummaries = CreateMockExecutionSummaries();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _consoleSummaryWriter.Write(Arg.Any<List<IClassExecutionSummary>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _executionSummaryWriter.Write(executionSummaries, cts.Token));
    }

    private static List<IClassExecutionSummary> CreateMockExecutionSummaries() =>
    [
        CreateMockExecutionSummary("TestClass1"),
        CreateMockExecutionSummary("TestClass2")
    ];

    private static IClassExecutionSummary CreateMockExecutionSummary(string className)
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        var testClass = Substitute.For<Type>();
        testClass.Name.Returns(className);
        summary.TestClass.Returns(testClass);

        var executionSettings = Substitute.For<IExecutionSettings>();
        summary.ExecutionSettings.Returns(executionSettings);

        summary.CompiledTestCaseResults.Returns(new List<ICompiledTestCaseResult>());

        return summary;
    }
}
