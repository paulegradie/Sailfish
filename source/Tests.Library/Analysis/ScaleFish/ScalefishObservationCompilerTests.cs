using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ScalefishObservationCompilerTests
{
    private readonly IScalefishObservationCompiler _compiler;

    public ScalefishObservationCompilerTests()
    {
        _compiler = new ScalefishObservationCompiler();
    }

    [Fact]
    public void CompileObservationSet_WithNoComplexityVariables_ReturnsNull()
    {
        // Arrange
        var summary = CreateMockExecutionSummary(typeof(TestClassWithoutComplexityVariables));

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CompileObservationSet_WithComplexityVariables_ReturnsObservationSet()
    {
        // Arrange
        var summary = CreateMockExecutionSummaryWithComplexityVariables();

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldNotBeNull();
        result.TestClassFullName.ShouldContain("TestClassWithComplexityVariables");
        result.Observations.ShouldNotBeEmpty();
    }

    [Fact]
    public void CompileObservationSet_WithSuccessfulTestCases_CreatesObservations()
    {
        // Arrange
        var summary = CreateMockExecutionSummaryWithComplexityVariables();

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldNotBeNull();
        result.Observations.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompileObservationSet_GroupsTestCasesByMethodName()
    {
        // Arrange
        var summary = CreateMockExecutionSummaryWithMultipleMethods();

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldNotBeNull();
        var methodNames = result.Observations.Select(o => o.MethodName).Distinct().ToList();
        methodNames.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void CompileObservationSet_CreatesComplexityMeasurements()
    {
        // Arrange
        var summary = CreateMockExecutionSummaryWithComplexityVariables();

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldNotBeNull();
        var firstObservation = result.Observations.First();
        firstObservation.ComplexityMeasurements.ShouldNotBeEmpty();
    }

    [Fact]
    public void CompileObservationSet_UsesTestClassFullNameForClassName()
    {
        // Arrange
        var summary = CreateMockExecutionSummaryWithComplexityVariables();

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldNotBeNull();
        result.TestClassFullName.ShouldContain("TestClassWithComplexityVariables");
    }

    [Fact]
    public void CompileObservationSet_WithNullFullName_UsesFallbackName()
    {
        // Arrange
        var summary = CreateMockExecutionSummary(typeof(TestClassWithComplexityVariables), useNullFullName: true);

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldNotBeNull();
        result.TestClassFullName.ShouldStartWith("Unknown-Namespace-");
    }

    [Fact]
    public void CompileObservationSet_FiltersForSuccessfulTestCases()
    {
        // Arrange
        var summary = CreateMockExecutionSummaryWithFailedTests();

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        // Should still return a result but only process successful test cases
        result.ShouldNotBeNull();
    }

    [Fact]
    public void CompileObservationSet_ExtractsVariablesFromAttribute()
    {
        // Arrange
        var summary = CreateMockExecutionSummaryWithComplexityVariables();

        // Act
        var result = _compiler.CompileObservationSet(summary);

        // Assert
        result.ShouldNotBeNull();
        var observation = result.Observations.First();
        observation.ComplexityMeasurements.Length.ShouldBe(3); // Based on our test data
    }

    private static IClassExecutionSummary CreateMockExecutionSummary(Type testClass, bool useNullFullName = false)
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(testClass);
        
        if (useNullFullName)
        {
            var mockType = Substitute.For<Type>();
            mockType.FullName.Returns((string?)null);
            mockType.Name.Returns(testClass.Name);
            mockType.GetProperties().Returns(testClass.GetProperties());
            summary.TestClass.Returns(mockType);
        }

        summary.CompiledTestCaseResults.Returns(new List<ICompiledTestCaseResult>());
        summary.FilterForSuccessfulTestCases().Returns(summary);

        return summary;
    }

    private static IClassExecutionSummary CreateMockExecutionSummaryWithComplexityVariables()
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(TestClassWithComplexityVariables));

        var testCaseResults = new List<ICompiledTestCaseResult>
        {
            CreateTestCaseResult("TestMethod", 1, 100.0),
            CreateTestCaseResult("TestMethod", 2, 200.0),
            CreateTestCaseResult("TestMethod", 3, 300.0)
        };

        summary.CompiledTestCaseResults.Returns(testCaseResults);
        summary.FilterForSuccessfulTestCases().Returns(summary);

        return summary;
    }

    private static IClassExecutionSummary CreateMockExecutionSummaryWithMultipleMethods()
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(TestClassWithComplexityVariables));

        var testCaseResults = new List<ICompiledTestCaseResult>
        {
            CreateTestCaseResult("Method1", 1, 100.0),
            CreateTestCaseResult("Method1", 2, 200.0),
            CreateTestCaseResult("Method1", 3, 300.0),
            CreateTestCaseResult("Method2", 1, 150.0),
            CreateTestCaseResult("Method2", 2, 250.0),
            CreateTestCaseResult("Method2", 3, 350.0)
        };

        summary.CompiledTestCaseResults.Returns(testCaseResults);
        summary.FilterForSuccessfulTestCases().Returns(summary);

        return summary;
    }

    private static IClassExecutionSummary CreateMockExecutionSummaryWithFailedTests()
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(TestClassWithComplexityVariables));

        var successfulResults = new List<ICompiledTestCaseResult>
        {
            CreateTestCaseResult("TestMethod", 1, 100.0),
            CreateTestCaseResult("TestMethod", 2, 200.0),
            CreateTestCaseResult("TestMethod", 3, 300.0)
        };

        var failedResult = Substitute.For<ICompiledTestCaseResult>();
        failedResult.PerformanceRunResult.Returns((PerformanceRunResult?)null);
        failedResult.Exception.Returns(new Exception("Test failed"));

        var allResults = new List<ICompiledTestCaseResult>(successfulResults) { failedResult };
        summary.CompiledTestCaseResults.Returns(allResults);

        var filteredSummary = Substitute.For<IClassExecutionSummary>();
        filteredSummary.TestClass.Returns(typeof(TestClassWithComplexityVariables));
        filteredSummary.CompiledTestCaseResults.Returns(successfulResults);

        summary.FilterForSuccessfulTestCases().Returns(filteredSummary);

        return summary;
    }

    private static ICompiledTestCaseResult CreateTestCaseResult(string methodName, int variableValue, double meanTime)
    {
        var testCaseId = TestCaseIdBuilder.Create()
            .WithTestCaseName(methodName)
            .Build();

        var performanceResult = PerformanceRunResultBuilder.Create()
            .WithDisplayName(testCaseId.DisplayName)
            .WithMean(meanTime)
            .Build();

        var result = Substitute.For<ICompiledTestCaseResult>();
        result.TestCaseId.Returns(testCaseId);
        result.PerformanceRunResult.Returns(performanceResult);

        return result;
    }

    private class TestClassWithoutComplexityVariables
    {
        [SailfishVariable(1, 2, 3)]
        public int N { get; set; }
    }

    private class TestClassWithComplexityVariables
    {
        [SailfishVariable(true, 1, 2, 3)]
        public int N { get; set; }
    }
}

