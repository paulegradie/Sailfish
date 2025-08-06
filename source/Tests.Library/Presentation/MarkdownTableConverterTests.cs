using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

/// <summary>
/// Comprehensive unit tests for MarkdownTableConverter.
/// Tests markdown generation, filtering, enhanced formatting, and ScaleFish result conversion.
/// </summary>
public class MarkdownTableConverterTests
{
    private readonly MarkdownTableConverter markdownTableConverter;
    private readonly ISailDiffUnifiedFormatter mockUnifiedFormatter;

    public MarkdownTableConverterTests()
    {
        mockUnifiedFormatter = Substitute.For<ISailDiffUnifiedFormatter>();
        markdownTableConverter = new MarkdownTableConverter(mockUnifiedFormatter);
    }

    [Fact]
    public void Constructor_WithoutFormatter_ShouldCreateInstance()
    {
        // Act
        var converter = new MarkdownTableConverter();

        // Assert
        converter.ShouldNotBeNull();
        converter.ShouldBeAssignableTo<IMarkdownTableConverter>();
    }

    [Fact]
    public void Constructor_WithFormatter_ShouldCreateInstance()
    {
        // Act & Assert
        markdownTableConverter.ShouldNotBeNull();
        markdownTableConverter.ShouldBeAssignableTo<IMarkdownTableConverter>();
    }

    [Fact]
    public void Constructor_WithNullFormatter_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MarkdownTableConverter(null!));
    }

    [Fact]
    public void ConvertToMarkdownTableString_WithEmptyList_ShouldReturnEmptyString()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>();

        // Act
        var result = markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ConvertToMarkdownTableString_WithValidSummaries_ShouldGenerateMarkdown()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>
        {
            CreateMockExecutionSummary("TestClass1"),
            CreateMockExecutionSummary("TestClass2")
        };

        // Act
        var result = markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldContain("TestClass1");
        result.ShouldContain("TestClass2");
    }

    [Fact]
    public void ConvertToMarkdownTableString_WithFilter_ShouldApplyFilter()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>
        {
            CreateMockExecutionSummary("TestClass1"),
            CreateMockExecutionSummary("TestClass2")
        };

        // Act
        var result = markdownTableConverter.ConvertToMarkdownTableString(
            executionSummaries, 
            summary => summary.TestClass.Name == "TestClass1");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("TestClass1");
        result.ShouldNotContain("TestClass2");
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTableString_WithEmptyList_ShouldReturnNoResultsMessage()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>();

        // Act
        var result = markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("No performance test results available.");
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTableString_WithValidSummaries_ShouldGenerateEnhancedMarkdown()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>
        {
            CreateMockExecutionSummary("TestClass1")
        };

        // Act
        var result = markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldContain("🧪 TestClass1"); // Enhanced formatting with emoji
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTableString_WithFilter_ShouldApplyFilter()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>
        {
            CreateMockExecutionSummary("TestClass1"),
            CreateMockExecutionSummary("TestClass2")
        };

        // Act
        var result = markdownTableConverter.ConvertToEnhancedMarkdownTableString(
            executionSummaries,
            summary => summary.TestClass.Name == "TestClass1");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("🧪 TestClass1");
        result.ShouldNotContain("🧪 TestClass2");
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTableString_WithNoResults_ShouldShowNoResultsMessage()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummary("TestClass1", hasResults: false);
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var result = markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("No performance test results available.");
    }

    [Fact]
    public void ConvertScaleFishResultToMarkdown_WithEmptyList_ShouldReturnEmptyString()
    {
        // Arrange
        var scaleFishResults = new List<ScalefishClassModel>();

        // Act
        var result = markdownTableConverter.ConvertScaleFishResultToMarkdown(scaleFishResults);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ConvertScaleFishResultToMarkdown_WithValidResults_ShouldGenerateMarkdown()
    {
        // Arrange
        var scaleFishResults = new List<ScalefishClassModel>
        {
            CreateMockScaleFishClassModel()
        };

        // Act
        var result = markdownTableConverter.ConvertScaleFishResultToMarkdown(scaleFishResults);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldContain("Namespace:");
        result.ShouldContain("Test Class:");
    }

    [Fact]
    public void ConvertToMarkdownTableString_WithExceptions_ShouldIncludeExceptionSection()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummaryWithException("TestClass1");
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var result = markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("Exception"); // Should include exception information
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTableString_WithExceptions_ShouldIncludeExceptionSection()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummaryWithException("TestClass1");
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var result = markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("Exception"); // Should include exception information
    }

    [Fact]
    public void ConvertToMarkdownTableString_WithMultipleGroups_ShouldHandleGrouping()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummaryWithMultipleGroups("TestClass1");
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var result = markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldContain("TestClass1");
    }

    [Fact]
    public void ConvertScaleFishResultToMarkdown_WithMultipleMethodGroups_ShouldHandleGrouping()
    {
        // Arrange
        var scaleFishResult = CreateMockScaleFishClassModelWithMultipleMethods();
        var scaleFishResults = new List<ScalefishClassModel> { scaleFishResult };

        // Act
        var result = markdownTableConverter.ConvertScaleFishResultToMarkdown(scaleFishResults);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldContain("Method1");
        result.ShouldContain("Method2");
    }

    private IClassExecutionSummary CreateMockExecutionSummary(string className, bool hasResults = true)
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        var testClass = Substitute.For<Type>();
        testClass.Name.Returns(className);
        summary.TestClass.Returns(testClass);

        if (hasResults)
        {
            var testCaseResult = CreateMockTestCaseResult();
            summary.CompiledTestCaseResults.Returns(new List<ICompiledTestCaseResult> { testCaseResult });
        }
        else
        {
            summary.CompiledTestCaseResults.Returns(new List<ICompiledTestCaseResult>());
        }

        return summary;
    }

    private IClassExecutionSummary CreateMockExecutionSummaryWithException(string className)
    {
        var summary = CreateMockExecutionSummary(className);
        var testCaseResult = CreateMockTestCaseResultWithException();
        summary.CompiledTestCaseResults.Returns(new List<ICompiledTestCaseResult> { testCaseResult });
        return summary;
    }

    private IClassExecutionSummary CreateMockExecutionSummaryWithMultipleGroups(string className)
    {
        var summary = CreateMockExecutionSummary(className, false);
        var testCaseResults = new List<ICompiledTestCaseResult>
        {
            CreateMockTestCaseResult("Group1"),
            CreateMockTestCaseResult("Group2")
        };
        summary.CompiledTestCaseResults.Returns(testCaseResults);
        return summary;
    }

    private ICompiledTestCaseResult CreateMockTestCaseResult(string? groupingId = "TestGroup")
    {
        var result = Substitute.For<ICompiledTestCaseResult>();
        result.GroupingId.Returns(groupingId);
        result.Exception.Returns((Exception?)null);
        
        var performanceResult = CreateMockPerformanceRunResult();
        result.PerformanceRunResult.Returns(performanceResult);
        
        var testCaseId = CreateMockTestCaseId();
        result.TestCaseId.Returns(testCaseId);
        
        return result;
    }

    private ICompiledTestCaseResult CreateMockTestCaseResultWithException()
    {
        var result = CreateMockTestCaseResult();
        result.Exception.Returns(new InvalidOperationException("Test exception"));
        return result;
    }

    private PerformanceRunResult CreateMockPerformanceRunResult()
    {
        return new PerformanceRunResult(
            "TestMethod",
            100.5,
            5.2,
            27.04,
            99.8,
            new double[] { 95.0, 100.0, 105.0, 98.0, 102.0, 99.0, 101.0, 97.0, 103.0, 100.0 },
            10,
            2,
            new double[] { 95.0, 100.0, 105.0, 98.0, 102.0, 99.0, 101.0, 97.0, 103.0, 100.0 },
            new double[0],
            new double[0],
            0);
    }

    private TestCaseId CreateMockTestCaseId()
    {
        return new TestCaseId("TestMethod");
    }

    private ScalefishClassModel CreateMockScaleFishClassModel()
    {
        var methodModel = CreateMockScaleFishMethodModel("Method1");
        return new ScalefishClassModel("TestNamespace", "TestClass", new List<ScaleFishMethodModel> { methodModel });
    }

    private ScalefishClassModel CreateMockScaleFishClassModelWithMultipleMethods()
    {
        var methodModels = new List<ScaleFishMethodModel>
        {
            CreateMockScaleFishMethodModel("Method1"),
            CreateMockScaleFishMethodModel("Method2")
        };
        return new ScalefishClassModel("TestNamespace", "TestClass", methodModels);
    }

    private ScaleFishMethodModel CreateMockScaleFishMethodModel(string methodName)
    {
        var propertyModel = CreateMockScaleFishPropertyModel();
        return new ScaleFishMethodModel(methodName, new List<ScaleFishPropertyModel> { propertyModel });
    }

    private ScaleFishPropertyModel CreateMockScaleFishPropertyModel()
    {
        var scaleFishModel = CreateMockScaleFishModel();
        return new ScaleFishPropertyModel("Property1", scaleFishModel);
    }

    private ScaleFishModel CreateMockScaleFishModel()
    {
        var mockFunction = Substitute.For<ScaleFishModelFunction>();
        mockFunction.Name.Returns("Linear");
        var mockNextClosestFunction = Substitute.For<ScaleFishModelFunction>();
        mockNextClosestFunction.Name.Returns("Quadratic");
        return new ScaleFishModel(mockFunction, 0.95, mockNextClosestFunction, 0.85);
    }
}
