using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.TestAdapter.Queue.Processors.MethodComparison;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;

namespace Tests.TestAdapter.Queue;

public class CombinedClassExecutionSummaryTests
{
    private readonly Type _testClass = typeof(CombinedClassExecutionSummaryTests);
    private readonly IExecutionSettings _executionSettings = new ExecutionSettings();

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var testCaseId = Some.SimpleTestCaseId();
        var perfResult = PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId.DisplayName).Build();
        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(testCaseId, string.Empty, perfResult)
        };

        // Act
        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Assert
        summary.TestClass.ShouldBe(_testClass);
        summary.ExecutionSettings.ShouldBe(_executionSettings);
        summary.CompiledTestCaseResults.ShouldBe(compiledResults);
    }

    [Fact]
    public void GetSuccessfulTestCases_ShouldReturnOnlyTestCasesWithPerformanceResults()
    {
        // Arrange
        var successTestCaseId1 = Some.SimpleTestCaseId();
        var successTestCaseId2 = Some.SimpleTestCaseId();
        var failedTestCaseId = Some.SimpleTestCaseId();

        var perfResult1 = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId1.DisplayName).Build();
        var perfResult2 = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId2.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(successTestCaseId1, string.Empty, perfResult1),
            new CompiledTestCaseResult(successTestCaseId2, string.Empty, perfResult2),
            new CompiledTestCaseResult(failedTestCaseId, string.Empty, new Exception("Test failed"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var successfulTestCases = summary.GetSuccessfulTestCases().ToList();

        // Assert
        successfulTestCases.Count.ShouldBe(2);
        successfulTestCases.All(x => x.PerformanceRunResult is not null).ShouldBeTrue();
    }

    [Fact]
    public void GetSuccessfulTestCases_WhenNoSuccessfulTests_ShouldReturnEmptyCollection()
    {
        // Arrange
        var failedTestCaseId1 = Some.SimpleTestCaseId();
        var failedTestCaseId2 = Some.SimpleTestCaseId();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(failedTestCaseId1, string.Empty, new Exception("Test failed 1")),
            new CompiledTestCaseResult(failedTestCaseId2, string.Empty, new Exception("Test failed 2"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var successfulTestCases = summary.GetSuccessfulTestCases().ToList();

        // Assert
        successfulTestCases.ShouldBeEmpty();
    }

    [Fact]
    public void GetFailedTestCases_ShouldReturnOnlyTestCasesWithoutPerformanceResults()
    {
        // Arrange
        var successTestCaseId = Some.SimpleTestCaseId();
        var failedTestCaseId1 = Some.SimpleTestCaseId();
        var failedTestCaseId2 = Some.SimpleTestCaseId();

        var perfResult = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(successTestCaseId, string.Empty, perfResult),
            new CompiledTestCaseResult(failedTestCaseId1, string.Empty, new Exception("Test failed 1")),
            new CompiledTestCaseResult(failedTestCaseId2, string.Empty, new Exception("Test failed 2"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var failedTestCases = summary.GetFailedTestCases().ToList();

        // Assert
        failedTestCases.Count.ShouldBe(2);
        failedTestCases.All(x => x.PerformanceRunResult is null).ShouldBeTrue();
    }

    [Fact]
    public void GetFailedTestCases_WhenNoFailedTests_ShouldReturnEmptyCollection()
    {
        // Arrange
        var successTestCaseId1 = Some.SimpleTestCaseId();
        var successTestCaseId2 = Some.SimpleTestCaseId();

        var perfResult1 = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId1.DisplayName).Build();
        var perfResult2 = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId2.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(successTestCaseId1, string.Empty, perfResult1),
            new CompiledTestCaseResult(successTestCaseId2, string.Empty, perfResult2)
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var failedTestCases = summary.GetFailedTestCases().ToList();

        // Assert
        failedTestCases.ShouldBeEmpty();
    }

    [Fact]
    public void FilterForSuccessfulTestCases_ShouldReturnNewSummaryWithOnlySuccessfulTests()
    {
        // Arrange
        var successTestCaseId = Some.SimpleTestCaseId();
        var failedTestCaseId = Some.SimpleTestCaseId();

        var perfResult = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(successTestCaseId, string.Empty, perfResult),
            new CompiledTestCaseResult(failedTestCaseId, string.Empty, new Exception("Test failed"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var filteredSummary = summary.FilterForSuccessfulTestCases();

        // Assert
        filteredSummary.ShouldNotBeNull();
        filteredSummary.ShouldBeOfType<CombinedClassExecutionSummary>();
        filteredSummary.TestClass.ShouldBe(_testClass);
        filteredSummary.ExecutionSettings.ShouldBe(_executionSettings);
        filteredSummary.CompiledTestCaseResults.Count().ShouldBe(1);
        filteredSummary.CompiledTestCaseResults.All(x => x.PerformanceRunResult is not null).ShouldBeTrue();
    }

    [Fact]
    public void FilterForSuccessfulTestCases_ShouldNotModifyOriginalSummary()
    {
        // Arrange
        var successTestCaseId = Some.SimpleTestCaseId();
        var failedTestCaseId = Some.SimpleTestCaseId();

        var perfResult = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(successTestCaseId, string.Empty, perfResult),
            new CompiledTestCaseResult(failedTestCaseId, string.Empty, new Exception("Test failed"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);
        var originalCount = summary.CompiledTestCaseResults.Count();

        // Act
        var filteredSummary = summary.FilterForSuccessfulTestCases();

        // Assert
        summary.CompiledTestCaseResults.Count().ShouldBe(originalCount);
        filteredSummary.CompiledTestCaseResults.Count().ShouldBeLessThan(originalCount);
    }

    [Fact]
    public void FilterForFailureTestCases_ShouldReturnNewSummaryWithOnlyFailedTests()
    {
        // Arrange
        var successTestCaseId = Some.SimpleTestCaseId();
        var failedTestCaseId = Some.SimpleTestCaseId();

        var perfResult = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(successTestCaseId, string.Empty, perfResult),
            new CompiledTestCaseResult(failedTestCaseId, string.Empty, new Exception("Test failed"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var filteredSummary = summary.FilterForFailureTestCases();

        // Assert
        filteredSummary.ShouldNotBeNull();
        filteredSummary.ShouldBeOfType<CombinedClassExecutionSummary>();
        filteredSummary.TestClass.ShouldBe(_testClass);
        filteredSummary.ExecutionSettings.ShouldBe(_executionSettings);
        filteredSummary.CompiledTestCaseResults.Count().ShouldBe(1);
        filteredSummary.CompiledTestCaseResults.All(x => x.PerformanceRunResult is null).ShouldBeTrue();
    }

    [Fact]
    public void FilterForFailureTestCases_ShouldNotModifyOriginalSummary()
    {
        // Arrange
        var successTestCaseId = Some.SimpleTestCaseId();
        var failedTestCaseId = Some.SimpleTestCaseId();

        var perfResult = PerformanceRunResultBuilder.Create().WithDisplayName(successTestCaseId.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(successTestCaseId, string.Empty, perfResult),
            new CompiledTestCaseResult(failedTestCaseId, string.Empty, new Exception("Test failed"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);
        var originalCount = summary.CompiledTestCaseResults.Count();

        // Act
        var filteredSummary = summary.FilterForFailureTestCases();

        // Assert
        summary.CompiledTestCaseResults.Count().ShouldBe(originalCount);
        filteredSummary.CompiledTestCaseResults.Count().ShouldBeLessThan(originalCount);
    }

    [Fact]
    public void FilterForSuccessfulTestCases_WhenAllTestsSuccessful_ShouldReturnAllTests()
    {
        // Arrange
        var testCaseId1 = Some.SimpleTestCaseId();
        var testCaseId2 = Some.SimpleTestCaseId();

        var perfResult1 = PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId1.DisplayName).Build();
        var perfResult2 = PerformanceRunResultBuilder.Create().WithDisplayName(testCaseId2.DisplayName).Build();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(testCaseId1, string.Empty, perfResult1),
            new CompiledTestCaseResult(testCaseId2, string.Empty, perfResult2)
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var filteredSummary = summary.FilterForSuccessfulTestCases();

        // Assert
        filteredSummary.CompiledTestCaseResults.Count().ShouldBe(2);
    }

    [Fact]
    public void FilterForFailureTestCases_WhenAllTestsFailed_ShouldReturnAllTests()
    {
        // Arrange
        var testCaseId1 = Some.SimpleTestCaseId();
        var testCaseId2 = Some.SimpleTestCaseId();

        var compiledResults = new List<ICompiledTestCaseResult>
        {
            new CompiledTestCaseResult(testCaseId1, string.Empty, new Exception("Test failed 1")),
            new CompiledTestCaseResult(testCaseId2, string.Empty, new Exception("Test failed 2"))
        };

        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, compiledResults);

        // Act
        var filteredSummary = summary.FilterForFailureTestCases();

        // Assert
        filteredSummary.CompiledTestCaseResults.Count().ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithEmptyCompiledResults_ShouldNotThrow()
    {
        // Arrange & Act
        var summary = new CombinedClassExecutionSummary(_testClass, _executionSettings, new List<ICompiledTestCaseResult>());

        // Assert
        summary.CompiledTestCaseResults.ShouldBeEmpty();
        summary.GetSuccessfulTestCases().ShouldBeEmpty();
        summary.GetFailedTestCases().ShouldBeEmpty();
    }
}

