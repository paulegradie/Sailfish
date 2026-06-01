using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Handlers.TestCaseEvents;
using Sailfish.TestAdapter.TestProperties;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.TestAdapter;

public class TestAdapterHandlerTests
{
    [Fact]
    public async Task TestCaseDisabledNotificationHandlerWorksAsExpected()
    {
        var frameworkWriter = Substitute.For<ITestFrameworkWriter>();
        var testCaseId = Some.SimpleTestCaseId();
        var instanceContainer = new TestInstanceContainerExternal(
            typeof(TestAdapterHandlerTests),
            new object(),
            typeof(TestAdapterHandlerTests).GetMethods().First(),
            testCaseId,
            new ExecutionSettings(),
            new PerformanceTimer(),
            false);

        var testCase = new TestCase();
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty, testCaseId.DisplayName);

        var notification = new TestCaseDisabledNotification(instanceContainer, new[] { testCase }, false);

        var handler = new TestCaseDisabledNotificationHandler(frameworkWriter);
        await handler.Handle(notification, CancellationToken.None);

        var calls = frameworkWriter.ReceivedCalls()?.ToList();
        calls.ShouldNotBeNull();
        calls.Count.ShouldBe(2);
        calls.First().GetMethodInfo().Name.ShouldBe("RecordEnd");
        calls.Last().GetMethodInfo().Name.ShouldBe("RecordResult");

        var recordEndArgs = calls.First().GetArguments();
        recordEndArgs.Length.ShouldBe(2);
        recordEndArgs.First().ShouldBe(testCase);
        recordEndArgs.Last().ShouldBe(TestOutcome.Skipped);

        var recordResultArgs = calls.Last().GetArguments();
        recordResultArgs.Length.ShouldBe(1);
        var result = recordResultArgs.First() as TestResult;
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe("Test Disabled");
    }

    [Fact]
    public async Task TestCaseExceptionNotificationHandlerTests()
    {
        var frameworkWriter = Substitute.For<ITestFrameworkWriter>();
        var logger = Substitute.For<ILogger>();
        var testCaseId = Some.SimpleTestCaseId();
        var instanceContainer = new TestInstanceContainerExternal(
            typeof(TestAdapterHandlerTests),
            new object(),
            typeof(TestAdapterHandlerTests).GetMethods().First(),
            testCaseId,
            new ExecutionSettings(),
            new PerformanceTimer(),
            false);

        var testCase = new TestCase();
        testCase.DisplayName = testCaseId.DisplayName;
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty, testCaseId.DisplayName);
        var notification = new TestCaseExceptionNotification(instanceContainer, new[] { testCase }, null);
        var handler = new TestCaseExceptionNotificationHandler(frameworkWriter, logger);

        await handler.Handle(notification, CancellationToken.None);

        var calls = frameworkWriter.ReceivedCalls()?.ToList();
        calls.ShouldNotBeNull();
        calls.Count.ShouldBe(2);
        calls.First().GetMethodInfo().Name.ShouldBe("RecordEnd");
        calls.Last().GetMethodInfo().Name.ShouldBe("RecordResult");

        var recordEndArgs = calls.First().GetArguments();
        recordEndArgs.Length.ShouldBe(2);
        recordEndArgs.First().ShouldBe(testCase);
        recordEndArgs.Last().ShouldBe(TestOutcome.Failed);

        var recordResultArgs = calls.Last().GetArguments();
        recordResultArgs.Length.ShouldBe(1);
        var result = recordResultArgs.First() as TestResult;
        result.ShouldNotBeNull();
        // The recorded TestResult must itself carry Outcome = Failed — recording it with the
        // default TestOutcome.None makes Rider report "Outcome value None is not understood".
        result.Outcome.ShouldBe(TestOutcome.Failed);
    }

    [Fact]
    public async Task TestCaseExceptionNotificationHandler_WithNullInstanceContainer_FailsEveryCaseWithFlattenedException()
    {
        // Whole-class activation failure: the test instance was never built (constructor threw,
        // a fixture failed to activate, or a constructor dependency couldn't be resolved), so the
        // engine publishes a notification with TestInstanceContainer == null and the wrapped exception.
        // Every test case in the group must be reported as Failed (not TestOutcome.None) with the
        // real, innermost reason flattened into ErrorMessage.
        var frameworkWriter = Substitute.For<ITestFrameworkWriter>();
        var logger = Substitute.For<ILogger>();

        var testCases = new[]
        {
            new TestCase(Some.RandomString(), TestExecutor.ExecutorUri, Some.RandomString()),
            new TestCase(Some.RandomString(), TestExecutor.ExecutorUri, Some.RandomString()),
            new TestCase(Some.RandomString(), TestExecutor.ExecutorUri, Some.RandomString())
        };

        // Mirror TypeActivator: the actionable cause ("Cannot reach the database...") is the
        // InnerException of a TestClassInstantiationException-style outer message.
        const string innerMessage = "Cannot reach the database — is it running?";
        const string outerMessage = "Failed to resolve constructor dependencies for test class 'MyBenchmarks'.";
        var exception = new InvalidOperationException(outerMessage, new InvalidOperationException(innerMessage));

        var notification = new TestCaseExceptionNotification(null, testCases, exception);
        var handler = new TestCaseExceptionNotificationHandler(frameworkWriter, logger);

        await handler.Handle(notification, CancellationToken.None);

        foreach (var testCase in testCases)
        {
            frameworkWriter.Received(1).RecordEnd(testCase, TestOutcome.Failed);
        }

        var recordedResults = frameworkWriter.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "RecordResult")
            .Select(call => call.GetArguments().First() as TestResult)
            .ToList();

        recordedResults.Count.ShouldBe(testCases.Length);
        foreach (var result in recordedResults)
        {
            result.ShouldNotBeNull();
            result.Outcome.ShouldBe(TestOutcome.Failed);
            // The chain must be flattened so the user sees both the wrapper and the real cause.
            result.ErrorMessage.ShouldNotBeNull();
            result.ErrorMessage.ShouldContain(outerMessage);
            result.ErrorMessage.ShouldContain(innerMessage);
            result.ErrorStackTrace.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task FrameworkTestCaseEndNotificationHandlerExceptionsAreHandled()
    {
        var frameworkWriter = Substitute.For<ITestFrameworkWriter>();

        var notification = new FrameworkTestCaseEndNotification(
            Some.RandomString(),
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            200.0,
            new TestCase(Some.RandomString(), TestExecutor.ExecutorUri, Some.RandomString()),
            StatusCode.Failure,
            new Exception("Test")
        );

        var handler = new FrameworkTestCaseEndNotificationHandler(frameworkWriter);

        await handler.Handle(notification, CancellationToken.None);

        var call = frameworkWriter.ReceivedCalls().Last();
        var testResult = call.GetArguments().First() as TestResult;
        if (testResult is null) Assert.Fail();
        testResult.ErrorMessage.ShouldBe("Test");
    }

    [Fact]
    public async Task FrameworkTestCaseEndNotificationHandlerExceptionsAreHandledWithInvocationError()
    {
        var frameworkWriter = Substitute.For<ITestFrameworkWriter>();
        var exc = new Exception();
        exc.SetStackTrace("InvocationReflectionExtensionMethods.TryInvoke");
        var notification = new FrameworkTestCaseEndNotification(
            Some.RandomString(),
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            200.0,
            new TestCase(Some.RandomString(), TestExecutor.ExecutorUri, Some.RandomString()),
            StatusCode.Failure,
            exc);

        var handler = new FrameworkTestCaseEndNotificationHandler(frameworkWriter);

        await handler.Handle(notification, CancellationToken.None);

        var call = frameworkWriter.ReceivedCalls().Last();
        var testResult = call.GetArguments().First() as TestResult;
        if (testResult is null) Assert.Fail();
        testResult.ErrorMessage.ShouldStartWith("An unhandled exception was thrown in your SailfishMethod");
    }
}