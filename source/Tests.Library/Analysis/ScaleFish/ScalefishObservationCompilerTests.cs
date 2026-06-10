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

    [Fact]
    public void CompileObservationSet_PairsMeasurementsWithDeclaredValues_RegardlessOfResultOrder()
    {
        // Results arrive in reverse declaration order. Positional indexing would pair X=1 with the
        // N=3 measurement; value-based selection must pair each X with its own case's mean.
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(TestClassWithComplexityVariables));
        var testCaseResults = new List<ICompiledTestCaseResult>
        {
            CreateTestCaseResult("TestMethod", 3, 300.0),
            CreateTestCaseResult("TestMethod", 1, 100.0),
            CreateTestCaseResult("TestMethod", 2, 200.0)
        };
        summary.CompiledTestCaseResults.Returns(testCaseResults);
        summary.FilterForSuccessfulTestCases().Returns(summary);

        var result = _compiler.CompileObservationSet(summary);

        result.ShouldNotBeNull();
        var measurements = result.Observations.Single().ComplexityMeasurements;
        measurements.Select(m => (m.X, m.Y)).ShouldBe(new[] { (1.0, 100.0), (2.0, 200.0), (3.0, 300.0) });
    }

    [Fact]
    public void CompileObservationSet_MiddleComplexityVariable_GetsItsOwnSeries()
    {
        // Regression: the old stride arithmetic compared a property's index against its own value
        // count (`index < VariableCount - 1`) instead of the property count. The ScaleFish attribute
        // requires ≥ 3 values, so the first affected position is property index 2 with exactly 3
        // values and at least one property after it: C's stride degenerated to 1 and its "series"
        // actually varied D. Each property's measurements must vary only that property, with every
        // other property held at its first declared value.
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(TestClassWithFourComplexityVariables));

        var testCaseResults = new List<ICompiledTestCaseResult>();
        foreach (var a in new[] { 1, 2, 3 })
        foreach (var b in new[] { 10, 20, 30 })
        foreach (var c in new[] { 100, 200, 300 })
        foreach (var d in new[] { 1000, 2000, 3000 })
            testCaseResults.Add(CreateTestCaseResult(
                "TestMethod",
                [
                    new TestCaseVariable("A", a),
                    new TestCaseVariable("B", b),
                    new TestCaseVariable("C", c),
                    new TestCaseVariable("D", d)
                ],
                EncodedMean(a, b, c, d)));

        summary.CompiledTestCaseResults.Returns(testCaseResults);
        summary.FilterForSuccessfulTestCases().Returns(summary);

        var result = _compiler.CompileObservationSet(summary);

        result.ShouldNotBeNull();
        var byProperty = result.Observations.ToDictionary(o => o.PropertyName, o => o.ComplexityMeasurements);

        byProperty["A"].Select(m => (m.X, m.Y))
            .ShouldBe(new[] { 1, 2, 3 }.Select(a => ((double)a, EncodedMean(a, 10, 100, 1000))));
        byProperty["B"].Select(m => (m.X, m.Y))
            .ShouldBe(new[] { 10, 20, 30 }.Select(b => ((double)b, EncodedMean(1, b, 100, 1000))));
        byProperty["C"].Select(m => (m.X, m.Y))
            .ShouldBe(new[] { 100, 200, 300 }.Select(c => ((double)c, EncodedMean(1, 10, c, 1000))));
        byProperty["D"].Select(m => (m.X, m.Y))
            .ShouldBe(new[] { 1000, 2000, 3000 }.Select(d => ((double)d, EncodedMean(1, 10, 100, d))));
    }

    [Fact]
    public void CompileObservationSet_NonComplexityVariable_HeldFixedAcrossTheSeries()
    {
        // A plain (non-scaleFish) variable participates in the cartesian product. The complexity
        // series must hold it at a single value rather than mixing measurements across its values.
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(TestClassWithMixedVariables));

        var testCaseResults = new List<ICompiledTestCaseResult>();
        foreach (var size in new[] { 1, 2, 3 })
        foreach (var mode in new[] { "alpha", "beta" })
            testCaseResults.Add(CreateTestCaseResult(
                "TestMethod",
                [new TestCaseVariable("Mode", mode), new TestCaseVariable("Size", size)],
                size * 100.0 + (mode == "alpha" ? 0.0 : 1.0)));

        summary.CompiledTestCaseResults.Returns(testCaseResults);
        summary.FilterForSuccessfulTestCases().Returns(summary);

        var result = _compiler.CompileObservationSet(summary);

        result.ShouldNotBeNull();
        var measurements = result.Observations.Single(o => o.PropertyName == "Size").ComplexityMeasurements;
        measurements.Select(m => (m.X, m.Y)).ShouldBe(new[] { (1.0, 100.0), (2.0, 200.0), (3.0, 300.0) });
    }

    [Fact]
    public void CompileObservationSet_FilteredFailedCase_DoesNotShiftAttribution()
    {
        // The N=2 case failed and was filtered out. Positional indexing would walk off the end or
        // shift every later measurement onto the wrong X; value-based selection just omits the gap.
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(typeof(TestClassWithComplexityVariables));
        var testCaseResults = new List<ICompiledTestCaseResult>
        {
            CreateTestCaseResult("TestMethod", 1, 100.0),
            CreateTestCaseResult("TestMethod", 3, 300.0)
        };
        summary.CompiledTestCaseResults.Returns(testCaseResults);
        summary.FilterForSuccessfulTestCases().Returns(summary);

        var result = _compiler.CompileObservationSet(summary);

        result.ShouldNotBeNull();
        var measurements = result.Observations.Single().ComplexityMeasurements;
        measurements.Select(m => (m.X, m.Y)).ShouldBe(new[] { (1.0, 100.0), (3.0, 300.0) });
    }

    private static double EncodedMean(int a, int b, int c, int d) =>
        a * 1_000_000_000.0 + b * 1_000_000.0 + c * 1_000.0 + d;

    private static ICompiledTestCaseResult CreateTestCaseResult(string methodName, int variableValue, double meanTime)
    {
        return CreateTestCaseResult(methodName, [new TestCaseVariable("N", variableValue)], meanTime);
    }

    private static ICompiledTestCaseResult CreateTestCaseResult(
        string methodName,
        IEnumerable<TestCaseVariable> variables,
        double meanTime)
    {
        var testCaseId = TestCaseIdBuilder.Create()
            .WithTestCaseName(methodName)
            .WithTestCaseVariables(variables)
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

    private class TestClassWithFourComplexityVariables
    {
        [SailfishVariable(true, 1, 2, 3)]
        public int A { get; set; }

        [SailfishVariable(true, 10, 20, 30)]
        public int B { get; set; }

        // Three values (the attribute minimum) at property index 2: the old
        // `index < own-value-count - 1` stride condition degenerated to stride 1 here,
        // attributing D-varying cases to C.
        [SailfishVariable(true, 100, 200, 300)]
        public int C { get; set; }

        [SailfishVariable(true, 1000, 2000, 3000)]
        public int D { get; set; }
    }

    private class TestClassWithMixedVariables
    {
        [SailfishVariable(true, 1, 2, 3)]
        public int Size { get; set; }

        [SailfishVariable("alpha", "beta")]
        public string Mode { get; set; } = string.Empty;
    }
}

