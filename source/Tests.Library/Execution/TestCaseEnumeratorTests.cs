using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.Presentation.Console;
using Shouldly;
using Tests.Common.Utils;
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
            MemoryCache.Default,
            Some.RandomString(),
            [],
            CancellationToken.None);

        result.Count.ShouldBe(1);
        result.Single().IsSuccess.ShouldBeFalse();
        result.Single().Exception?.Message.ShouldBe("Failed to create test cases for Tests.Library.Execution.TestCaseEnumerationTests");
        enumerator.Dispose();
    }
}