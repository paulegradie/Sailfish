using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.Handlers.TestCaseEvents;
using Sailfish.TestAdapter.TestProperties;
using Shouldly;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tests.TestAdapter.Utils;
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
        var result = (recordResultArgs.First() as TestResult);
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
        var result = (recordResultArgs.First() as TestResult);
        result.ShouldNotBeNull();
    }
}