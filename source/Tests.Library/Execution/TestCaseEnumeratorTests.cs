using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation.Console;
using Shouldly;
using Tests.Common.Utils;
using Tests.E2E.TestSuite.Discoverable;
using Xunit;

namespace Tests.Library.Execution;

public class TestCaseEnumerationTests
{
    [Fact]
    public async Task ExceptionsOnTestCaseEnumerationAreHandled()
    {
        var logger = Substitute.For<ILogger>();
        var consoleWriter = Substitute.For<IConsoleWriter>();
        var iterator = Substitute.For<ITestCaseIterator>();
        var printer = Substitute.For<ITestCaseCountPrinter>();
        var mediator = Substitute.For<IMediator>();
        var summaryCompiler = Substitute.For<IClassExecutionSummaryCompiler>();
        var settings = Substitute.For<IRunSettings>();
        var engine = new SailfishExecutionEngine(logger, consoleWriter, iterator, printer, mediator, summaryCompiler, settings);
        var executionState = new ExecutionState();
        var enumerator = Substitute.For<IEnumerator<TestInstanceContainer>>();
        enumerator.MoveNext().Throws<Exception>();
        var instanceContainer = TestInstanceContainer.CreateTestInstance(
            new object(),
            typeof(TestCaseEnumerationTests).GetMethods().First(),
            [],
            [],
            false,
            new ExecutionSettings());
        enumerator.Current.Returns(instanceContainer);

        var testCaseEnumerator = Substitute.For<IEnumerable<TestInstanceContainer>>();
        testCaseEnumerator.GetEnumerator().Returns(enumerator);

        var provider = Substitute.For<ITestInstanceContainerProvider>();
        provider.Test.Returns(typeof(TestCaseEnumerationTests));
        provider.ProvideNextTestCaseEnumeratorForClass().Returns(testCaseEnumerator);

        var result = await engine.ActivateContainer(
            1,
            2,
            provider,
            executionState,
            Some.RandomString(),
            [],
            CancellationToken.None);

        result.Count.ShouldBe(1);
        result.Single().IsSuccess.ShouldBeFalse();
        result.Single().Exception?.Message.ShouldBe("Failed to create test cases for Tests.Library.Execution.TestCaseEnumerationTests");
        enumerator.Dispose();
    }

    [Fact]
    public async Task IterationCompletes()
    {
        var logger = Substitute.For<ILogger>();
        var consoleWriter = Substitute.For<IConsoleWriter>();
        var printer = Substitute.For<ITestCaseCountPrinter>();
        var mediator = Substitute.For<IMediator>();
        var settings = Substitute.For<IRunSettings>();

        var runSettings = new RunSettings(new[]
            {
                "MinimalTest"
            },
            ".",
            true,
            true,
            true,
            new SailDiffSettings(),
            new OrderedDictionary(),
            new OrderedDictionary(),
            new List<string>(),
            new List<Type>(),
            new List<Type>(),
            null,
            sampleSizeOverride: 3);

        var iterator = new TestCaseIterator(runSettings, logger);
        var summaryCompiler = new ClassExecutionSummaryCompiler(new StatisticsCompiler(), runSettings); //Substitute.For<IClassExecutionSummaryCompiler>());
        var engine = new SailfishExecutionEngine(logger, consoleWriter, iterator, printer, mediator, summaryCompiler, settings);
        var executionState = new ExecutionState();

        var provider = new TestInstanceContainerProvider(
            runSettings,
            new TypeActivator(
                Substitute.For<ILifetimeScope>()),
            typeof(MinimalTest),
            new List<PropertySet>(),
            typeof(MinimalTest).GetMethods().First());

        var result = await engine.ActivateContainer(
            1,
            2,
            provider,
            executionState,
            Some.RandomString(),
            [],
            CancellationToken.None);

        var sut = result.Single();
        sut.IsSuccess.ShouldBeTrue();
        sut.TestInstanceContainer.ShouldNotBeNull();
        sut.TestInstanceContainer.GroupingId.ShouldBe("MinimalTest.Minimal");
        sut.TestInstanceContainer.TestCaseId.DisplayName.ShouldBe("MinimalTest.Minimal()");
    }
}