using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.ScaleFish;
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
    private readonly MarkdownTableConverter _markdownTableConverter;
    private readonly ISailDiffUnifiedFormatter _mockUnifiedFormatter;

    public MarkdownTableConverterTests()
    {
        _mockUnifiedFormatter = Substitute.For<ISailDiffUnifiedFormatter>();
        _markdownTableConverter = new MarkdownTableConverter(_mockUnifiedFormatter);
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
        _markdownTableConverter.ShouldNotBeNull();
        _markdownTableConverter.ShouldBeAssignableTo<IMarkdownTableConverter>();
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
        var result = _markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertToMarkdownTableString(
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
        var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(
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
        var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertScaleFishResultToMarkdown(scaleFishResults);

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
        var result = _markdownTableConverter.ConvertScaleFishResultToMarkdown(scaleFishResults);

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
        var result = _markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertToMarkdownTableString(executionSummaries);

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
        var result = _markdownTableConverter.ConvertScaleFishResultToMarkdown(scaleFishResults);

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
            [95.0, 100.0, 105.0, 98.0, 102.0, 99.0, 101.0, 97.0, 103.0, 100.0],
            10,
            2,
            [95.0, 100.0, 105.0, 98.0, 102.0, 99.0, 101.0, 97.0, 103.0, 100.0],
            [],
            [],
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


        [Fact]
        public void ConvertToEnhancedMarkdownTableString_ShouldIncludeHeaderAndGenerated()
        {
            // Arrange
            var executionSummaries = new List<IClassExecutionSummary>
            {
                CreateMockExecutionSummary("TestClass1")
            };

            // Act
            var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

            // Assert
            result.ShouldContain("# 📊 Performance Test Results");
            result.ShouldContain("Generated:");
        }

        [Fact]
        public void ConvertToEnhancedMarkdownTableString_ShouldIncludeGroupHeader()
        {
            // Arrange
            var executionSummaries = new List<IClassExecutionSummary>
            {
                CreateMockExecutionSummary("TestClass1")
            };

            // Act
            var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

            // Assert
            result.ShouldContain("📈 TestGroup");
        }

        [Fact]
        public void ConvertToEnhancedMarkdownTableString_ShouldIncludeCISummary_WithMultiLevelAndAdaptiveFormatting()
        {
            // Arrange
            var ciList = new List<ConfidenceIntervalResult>
            {
                new(0.95, 0.12345678, 0, 0),
                new(0.99, 0.0000000001, 0, 0)
            };

            var compiled = CreateCompiledResult(
                groupingId: "GroupA",
                displayName: "MyTest",
                mean: 100.0,
                median: 100.0,
                stdDev: 1.0,
                variance: 1.0,
                sampleSize: 10,
                confidenceLevel: 0.95,
                marginOfError: 0.0,
                confidenceIntervals: ciList);

            var summary = CreateExecutionSummaryFromResults("TestClass1", compiled);
            var executionSummaries = new List<IClassExecutionSummary> { summary };

            // Act
            var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

            // Assert
            result.ShouldContain("- MyTest():");
            result.ShouldContain("CI ± 0.1235ms");
            result.ShouldContain("CI ± 0ms");
        }

        [Fact]
        public void ConvertToEnhancedMarkdownTableString_ShouldFallbackToSingleLevelCI_WhenNoMultiLevelProvided()
        {
            // Arrange: No ConfidenceIntervals provided; use legacy single-level fields
            var compiled = CreateCompiledResult(
                groupingId: "GroupA",
                displayName: "LegacyTest",
                mean: 100.0,
                median: 100.0,
                stdDev: 1.0,
                variance: 1.0,
                sampleSize: 10,
                confidenceLevel: 0.95,
                marginOfError: 0.004, // Should format to 0.0040 via adaptive formatting
                confidenceIntervals: []);

            var summary = CreateExecutionSummaryFromResults("TestClass1", compiled);
            var executionSummaries = new List<IClassExecutionSummary> { summary };

            // Act
            var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

            // Assert
            result.ShouldContain("- LegacyTest():");
            result.ShouldContain("CI ± 0.0040ms");
        }

        [Fact]
        public void ConvertToEnhancedMarkdownTableString_PerformanceSummary_ShouldReportFastestSlowestAndGap()
        {
            // Arrange
            var fast = CreateCompiledResult(
                groupingId: "GroupX",
                displayName: "FastMethod",
                mean: 100.0,
                median: 100.0,
                stdDev: 1.0,
                variance: 1.0,
                sampleSize: 10);

            var slow = CreateCompiledResult(
                groupingId: "GroupX",
                displayName: "SlowMethod",
                mean: 200.0,
                median: 200.0,
                stdDev: 2.0,
                variance: 4.0,
                sampleSize: 10);

            var summary = CreateExecutionSummaryFromResults("TestClass1", fast, slow);
            var executionSummaries = new List<IClassExecutionSummary> { summary };

            // Act
            var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

            // Assert
            result.ShouldContain("📊 Performance Summary:");
            result.ShouldContain("**Fastest:** FastMethod() (100.000ms)");
            result.ShouldContain("**Slowest:** SlowMethod() (200.000ms)");
            result.ShouldContain("**Performance Gap:** 100.0% difference");
        }

        [Fact]
        public void ConvertToEnhancedMarkdownTableString_TableShouldIncludeSampleSizeInStdDevHeader()
        {
            // Arrange
            var compiled = CreateCompiledResult(
                groupingId: "GroupB",
                displayName: "MyTest",
                mean: 10.0,
                median: 10.0,
                stdDev: 1.0,
                variance: 1.0,
                sampleSize: 10);

            var summary = CreateExecutionSummaryFromResults("TestClass1", compiled);
            var executionSummaries = new List<IClassExecutionSummary> { summary };

            // Act
            var result = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(executionSummaries);

            // Assert
            result.ShouldContain("StdDev (N=10)");
        }

        private static IClassExecutionSummary CreateExecutionSummaryFromResults(string className, params ICompiledTestCaseResult[] results)
        {
            var summary = Substitute.For<IClassExecutionSummary>();
            var testClass = Substitute.For<Type>();
            testClass.Name.Returns(className);
            summary.TestClass.Returns(testClass);
            summary.CompiledTestCaseResults.Returns(results.ToList());
            return summary;
        }

        private static ICompiledTestCaseResult CreateCompiledResult(
            string groupingId,
            string displayName,
            double mean,
            double median,
            double stdDev,
            double variance,
            int sampleSize,
            double confidenceLevel = 0.95,
            double marginOfError = 0.0,
            IReadOnlyList<ConfidenceIntervalResult>? confidenceIntervals = null)
        {
            var perf = new PerformanceRunResult(
                displayName,
                mean,
                stdDev,
                variance,
                median,
                [],
                sampleSize,
                0,
                [1.0, 2.0],
                [],
                [],
                0,
                0.0,
                confidenceLevel,
                0.0,
                0.0,
                marginOfError,
                confidenceIntervals);

            var compiled = Substitute.For<ICompiledTestCaseResult>();
            compiled.GroupingId.Returns(groupingId);
            compiled.TestCaseId.Returns(new TestCaseId(displayName));
            compiled.PerformanceRunResult.Returns(perf);
            compiled.Exception.Returns((Exception?)null);
            return compiled;
        }


        private sealed class TestManifestProvider : Sailfish.Results.IReproducibilityManifestProvider
        {
            public Sailfish.Results.ReproducibilityManifest? Current { get; set; }
        }

        [Fact]
        public void EnhancedMarkdown_Includes_TimerCalibration_Header_WhenManifestHasCalibration()
        {
            // Arrange manifest with timer calibration
            var manifestProvider = new TestManifestProvider
            {
                Current = new Sailfish.Results.ReproducibilityManifest
                {
                    TimerCalibration = new Sailfish.Results.ReproducibilityManifest.TimerCalibrationSnapshot
                    {
                        StopwatchFrequency = 3_000_000,
                        ResolutionNs = 333.3,
                        MedianTicks = 4,
                        RsdPercent = 2.2,
                        JitterScore = 91,
                        Samples = 64,
                        Warmups = 16
                    }
                }
            };
            var converter = new MarkdownTableConverter(_mockUnifiedFormatter, manifestProvider);
            var summaries = new List<IClassExecutionSummary> { CreateMockExecutionSummary("CalibClass") };

            // Act
            var md = converter.ConvertToEnhancedMarkdownTableString(summaries);

            // Assert
            md.ShouldContain("## ⏱️ Timer Calibration");
            md.ShouldContain("freq=3000000 Hz");
            md.ShouldContain("res≈333 ns");
            md.ShouldContain("baseline=4 ticks");
            md.ShouldContain("RSD=2.2%");
            md.ShouldContain("Score=91/100");
            md.ShouldContain("N=64 (warmup 16)");
        }

        [Fact]
        public void EnhancedMarkdown_Excludes_TimerCalibration_Header_WhenManifestMissingCalibration()
        {
            var manifestProvider = new TestManifestProvider { Current = new Sailfish.Results.ReproducibilityManifest() };
            var converter = new MarkdownTableConverter(_mockUnifiedFormatter, manifestProvider);
            var summaries = new List<IClassExecutionSummary> { CreateMockExecutionSummary("NoCalibClass") };

            var md = converter.ConvertToEnhancedMarkdownTableString(summaries);

            md.ShouldNotContain("## ⏱️ Timer Calibration");
        }

        [Fact]
        public void EnhancedMarkdown_Includes_Seed_WhenManifestHasRandomizationSeed()
        {
            var manifestProvider = new TestManifestProvider
            {
                Current = new Sailfish.Results.ReproducibilityManifest
                {
                    Randomization = new Sailfish.Results.ReproducibilityManifest.RandomizationConfig
                    {
                        Seed = 123
                    }
                }
            };
            var converter = new MarkdownTableConverter(_mockUnifiedFormatter, manifestProvider);
            var summaries = new List<IClassExecutionSummary> { CreateMockExecutionSummary("SeededClass") };

            var md = converter.ConvertToEnhancedMarkdownTableString(summaries);

            md.ShouldContain("Seed: 123");
        }

        [Fact]
        public void EnhancedMarkdown_Excludes_Seed_WhenManifestHasNoSeed()
        {
            var manifestProvider = new TestManifestProvider { Current = new Sailfish.Results.ReproducibilityManifest() };
            var converter = new MarkdownTableConverter(_mockUnifiedFormatter, manifestProvider);
            var summaries = new List<IClassExecutionSummary> { CreateMockExecutionSummary("NoSeedClass") };

            var md = converter.ConvertToEnhancedMarkdownTableString(summaries);

            md.ShouldNotContain("Seed:");
        }




        [Fact]
        public void EnhancedMarkdown_Excludes_TimerCalibration_Header_WhenManifestProviderIsNull()
        {
            // Uses fixture converter without manifest provider
            var summaries = new List<IClassExecutionSummary> { CreateMockExecutionSummary("NoProviderClass") };
            var md = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(summaries);
            md.ShouldNotContain("## ⏱️ Timer Calibration");
        }

}
