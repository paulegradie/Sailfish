using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Attributes;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Xunit;

namespace Sailfish.Tests.Execution;

public class MethodComparisonCoordinatorTests
{
    private readonly IStatisticalTestComputer statisticalTestComputer;
    private readonly IPerformanceRunResultAggregator aggregator;
    private readonly MethodComparisonCoordinator coordinator;

    public MethodComparisonCoordinatorTests()
    {
        statisticalTestComputer = Substitute.For<IStatisticalTestComputer>();
        aggregator = Substitute.For<IPerformanceRunResultAggregator>();
        coordinator = new MethodComparisonCoordinator(statisticalTestComputer, aggregator);
    }

    [Fact]
    public async Task ExecuteComparisons_WithNoComparisonMethods_ReturnsEmptyList()
    {
        // Arrange
        var results = new List<TestCaseExecutionResult>
        {
            CreateTestCaseExecutionResult("Method1", null)
        };

        // Act
        var comparisonResults = await coordinator.ExecuteComparisons(results);

        // Assert
        comparisonResults.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteComparisons_WithSingleMethod_ReturnsEmptyList()
    {
        // Arrange
        var method = CreateMethodWithComparisonAttribute("TestGroup");
        var results = new List<TestCaseExecutionResult>
        {
            CreateTestCaseExecutionResult("Method1", method)
        };

        // Act
        var comparisonResults = await coordinator.ExecuteComparisons(results);

        // Assert
        comparisonResults.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteComparisons_WithTwoMethodsInSameGroup_ReturnsComparisonResult()
    {
        // Arrange
        var method1 = CreateMethodWithComparisonAttribute("TestGroup");
        var method2 = CreateMethodWithComparisonAttribute("TestGroup");
        
        var results = new List<TestCaseExecutionResult>
        {
            CreateTestCaseExecutionResult("Method1", method1),
            CreateTestCaseExecutionResult("Method2", method2)
        };

        var sailDiffResults = new List<SailDiffResult>
        {
            CreateSailDiffResult("Method1", "Method2", 0.05)
        };

        statisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(sailDiffResults);

        // Act
        var comparisonResults = await coordinator.ExecuteComparisons(results);

        // Assert
        comparisonResults.Should().HaveCount(1);
        var result = comparisonResults.First();
        result.ComparisonGroup.GroupName.Should().Be("TestGroup");
        result.MethodRankings.Should().HaveCount(2);
        result.PairwiseComparisons.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteComparisons_WithDifferentGroups_ReturnsMultipleResults()
    {
        // Arrange
        var method1 = CreateMethodWithComparisonAttribute("Group1");
        var method2 = CreateMethodWithComparisonAttribute("Group1");
        var method3 = CreateMethodWithComparisonAttribute("Group2");
        var method4 = CreateMethodWithComparisonAttribute("Group2");
        
        var results = new List<TestCaseExecutionResult>
        {
            CreateTestCaseExecutionResult("Method1", method1),
            CreateTestCaseExecutionResult("Method2", method2),
            CreateTestCaseExecutionResult("Method3", method3),
            CreateTestCaseExecutionResult("Method4", method4)
        };

        statisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(new List<SailDiffResult>());

        // Act
        var comparisonResults = await coordinator.ExecuteComparisons(results);

        // Assert
        comparisonResults.Should().HaveCount(2);
        comparisonResults.Select(r => r.ComparisonGroup.GroupName).Should().Contain("Group1", "Group2");
    }

    private static MethodInfo CreateMethodWithComparisonAttribute(string groupName)
    {
        var method = Substitute.For<MethodInfo>();
        var attribute = new SailfishMethodComparisonAttribute(groupName);
        
        method.GetCustomAttribute<SailfishMethodComparisonAttribute>().Returns(attribute);
        return method;
    }

    private static TestCaseExecutionResult CreateTestCaseExecutionResult(string methodName, MethodInfo? method)
    {
        var testCaseId = new TestCaseId(
            new TestCaseName($"TestClass.{methodName}"),
            "TestClass",
            methodName,
            new List<string>());

        var container = Substitute.For<TestInstanceContainer>();
        container.TestCaseId.Returns(testCaseId);
        container.ExecutionMethod.Returns(method);

        var performanceTimer = new PerformanceTimer();
        // Add some mock execution times
        for (int i = 0; i < 5; i++)
        {
            performanceTimer.ExecutionIterationPerformances.Add(
                new IterationPerformance(DateTime.UtcNow.Ticks, DateTime.UtcNow.AddMilliseconds(10).Ticks));
        }

        var executionSettings = new ExecutionSettings(
            numWarmupIterations: 1,
            sampleSize: 5,
            globalSetupMethod: null,
            globalTeardownMethod: null,
            iterationSetupMethod: null,
            iterationTeardownMethod: null);

        var result = Substitute.For<TestCaseExecutionResult>();
        result.TestInstanceContainer.Returns(container);
        result.PerformanceTimerResults.Returns(performanceTimer);
        result.ExecutionSettings.Returns(executionSettings);
        result.IsSuccess.Returns(true);

        return result;
    }

    private static SailDiffResult CreateSailDiffResult(string method1, string method2, double pValue)
    {
        var testCaseId = new TestCaseId(
            new TestCaseName($"TestClass.{method1}"),
            "TestClass",
            method1,
            new List<string>());

        var statisticalTestResult = Substitute.For<IStatisticalTestResult>();
        statisticalTestResult.PValue.Returns(pValue);

        var testResult = Substitute.For<TestResultWithOutlierAnalysis>();
        testResult.StatisticalTestResult.Returns(statisticalTestResult);

        var sailDiffResult = Substitute.For<SailDiffResult>();
        sailDiffResult.TestCaseId.Returns(testCaseId);
        sailDiffResult.TestResultsWithOutlierAnalysis.Returns(testResult);

        return sailDiffResult;
    }
}
