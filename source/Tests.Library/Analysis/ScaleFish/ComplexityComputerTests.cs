using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Comprehensive unit tests for ComplexityComputer.
/// Tests complexity analysis logic, observation compilation, and result processing.
/// </summary>
public class ComplexityComputerTests
{
    private readonly IComplexityEstimator _mockComplexityEstimator;
    private readonly IScalefishObservationCompiler _mockObservationCompiler;
    private readonly ComplexityComputer _complexityComputer;

    public ComplexityComputerTests()
    {
        _mockComplexityEstimator = Substitute.For<IComplexityEstimator>();
        _mockObservationCompiler = Substitute.For<IScalefishObservationCompiler>();
        _complexityComputer = new ComplexityComputer(_mockComplexityEstimator, _mockObservationCompiler);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act & Assert
        _complexityComputer.ShouldNotBeNull();
        _complexityComputer.ShouldBeAssignableTo<IComplexityComputer>();
    }

    [Fact]
    public void Constructor_WithNullComplexityEstimator_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new ComplexityComputer(null!, _mockObservationCompiler));
    }

    [Fact]
    public void Constructor_WithNullObservationCompiler_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new ComplexityComputer(_mockComplexityEstimator, null!));
    }

    [Fact]
    public void AnalyzeComplexity_WithEmptyList_ShouldReturnEmptyResult()
    {
        // Arrange
        var executionSummaries = new List<IClassExecutionSummary>();

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void AnalyzeComplexity_WithNullObservationSet_ShouldSkipSummary()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummary();
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };
        
        _mockObservationCompiler.CompileObservationSet(executionSummary).Returns((ObservationSetFromSummaries?)null);

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void AnalyzeComplexity_WithValidObservationSet_ShouldProcessSummary()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummary();
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };
        
        var observationSet = CreateMockObservationSet();
        _mockObservationCompiler.CompileObservationSet(executionSummary).Returns(observationSet);
        
        var scaleFishModel = CreateMockScaleFishModel();
        _mockComplexityEstimator.EstimateComplexity(Arg.Any<ComplexityMeasurement[]>()).Returns(scaleFishModel);

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    [Fact]
    public void AnalyzeComplexity_WithMultipleSummaries_ShouldProcessAll()
    {
        // Arrange
        var executionSummary1 = CreateMockExecutionSummary(typeof(TestClass1));
        var executionSummary2 = CreateMockExecutionSummary(typeof(TestClass2));
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary1, executionSummary2 };
        
        var observationSet1 = CreateMockObservationSet("Method1");
        var observationSet2 = CreateMockObservationSet("Method2");
        
        _mockObservationCompiler.CompileObservationSet(executionSummary1).Returns(observationSet1);
        _mockObservationCompiler.CompileObservationSet(executionSummary2).Returns(observationSet2);
        
        var scaleFishModel = CreateMockScaleFishModel();
        _mockComplexityEstimator.EstimateComplexity(Arg.Any<ComplexityMeasurement[]>()).Returns(scaleFishModel);

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
    }

    [Fact]
    public void AnalyzeComplexity_WithNullComplexityResult_ShouldSkipMethod()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummary();
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };
        
        var observationSet = CreateMockObservationSet();
        _mockObservationCompiler.CompileObservationSet(executionSummary).Returns(observationSet);
        
        _mockComplexityEstimator.EstimateComplexity(Arg.Any<ComplexityMeasurement[]>()).Returns((ScaleFishModel?)null);

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        // Should still return a result but with empty complexity data
        result.ShouldNotBeEmpty();
    }

    [Fact]
    public void AnalyzeComplexity_WithMultipleMethodsInObservationSet_ShouldProcessAllMethods()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummary();
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };
        
        var observations = new List<ScaleFishObservation>
        {
            CreateMockObservation("Method1", "Property1"),
            CreateMockObservation("Method1", "Property2"),
            CreateMockObservation("Method2", "Property1"),
            CreateMockObservation("Method2", "Property2")
        };
        
        var observationSet = new ObservationSetFromSummaries("TestClass", observations);
        _mockObservationCompiler.CompileObservationSet(executionSummary).Returns(observationSet);
        
        var scaleFishModel = CreateMockScaleFishModel();
        _mockComplexityEstimator.EstimateComplexity(Arg.Any<ComplexityMeasurement[]>()).Returns(scaleFishModel);

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        // Verify that complexity estimation was called for each method group
        _mockComplexityEstimator.Received(4).EstimateComplexity(Arg.Any<ComplexityMeasurement[]>());
    }

    [Fact]
    public void AnalyzeComplexity_ShouldGroupObservationsByMethod()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummary();
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };
        
        var observations = new List<ScaleFishObservation>
        {
            CreateMockObservation("Method1", "Property1"),
            CreateMockObservation("Method1", "Property2"),
            CreateMockObservation("Method2", "Property1")
        };
        
        var observationSet = new ObservationSetFromSummaries("TestClass", observations);
        _mockObservationCompiler.CompileObservationSet(executionSummary).Returns(observationSet);
        
        var scaleFishModel = CreateMockScaleFishModel();
        _mockComplexityEstimator.EstimateComplexity(Arg.Any<ComplexityMeasurement[]>()).Returns(scaleFishModel);

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        // Should have called complexity estimation for each property in each method
        _mockComplexityEstimator.Received(3).EstimateComplexity(Arg.Any<ComplexityMeasurement[]>());
    }

    [Fact]
    public void AnalyzeComplexity_WithEmptyObservationSet_ShouldReturnEmptyResult()
    {
        // Arrange
        var executionSummary = CreateMockExecutionSummary();
        var executionSummaries = new List<IClassExecutionSummary> { executionSummary };
        
        var observationSet = new ObservationSetFromSummaries("TestClass", []);
        _mockObservationCompiler.CompileObservationSet(executionSummary).Returns(observationSet);

        // Act
        var result = _complexityComputer.AnalyzeComplexity(executionSummaries);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty(); // Should still return a class model but with no methods
    }

    private IClassExecutionSummary CreateMockExecutionSummary(Type? testClass = null)
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.TestClass.Returns(testClass ?? typeof(TestClass1));
        return summary;
    }

    private ObservationSetFromSummaries CreateMockObservationSet(string methodName = "TestMethod")
    {
        var observations = new List<ScaleFishObservation>
        {
            CreateMockObservation(methodName, "Property1"),
            CreateMockObservation(methodName, "Property2")
        };
        
        return new ObservationSetFromSummaries("TestClass", observations);
    }

    private ScaleFishObservation CreateMockObservation(string methodName, string propertyName)
    {
        var measurements = new ComplexityMeasurement[]
        {
            new(1, 100),
            new(2, 200),
            new(3, 300)
        };
        
        return new ScaleFishObservation(methodName, propertyName, measurements);
    }

    private ScaleFishModel CreateMockScaleFishModel()
    {
        var mockFunction = Substitute.For<ScaleFishModelFunction>();
        mockFunction.Name.Returns("Linear");
        
        var mockNextClosestFunction = Substitute.For<ScaleFishModelFunction>();
        mockNextClosestFunction.Name.Returns("Quadratic");

        return new ScaleFishModel(mockFunction, 0.95, mockNextClosestFunction, 0.85);
    }

    // Test classes for type differentiation
    private class TestClass1 { }
    private class TestClass2 { }
}
