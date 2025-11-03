using System.Collections.Generic;
using NSubstitute;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class TestCaseComplexityGroupTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var methodName = "TestMethod";
        var testCaseResults = new List<ICompiledTestCaseResult>
        {
            CreateMockTestCaseResult(),
            CreateMockTestCaseResult()
        };

        // Act
        var group = new TestCaseComplexityGroup(methodName, testCaseResults);

        // Assert
        group.TestCaseMethodName.ShouldBe(methodName);
        group.TestCaseGroup.ShouldBe(testCaseResults);
        group.TestCaseGroup.Count.ShouldBe(2);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var testCaseResults = new List<ICompiledTestCaseResult> { CreateMockTestCaseResult() };

        var group1 = new TestCaseComplexityGroup("Method1", testCaseResults);
        var group2 = new TestCaseComplexityGroup("Method1", testCaseResults);
        var group3 = new TestCaseComplexityGroup("Method2", testCaseResults);

        // Act & Assert
        group1.ShouldBe(group2);
        group1.ShouldNotBe(group3);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        // Arrange
        var methodName = "TestMethod";
        var testCaseResults = new List<ICompiledTestCaseResult>
        {
            CreateMockTestCaseResult(),
            CreateMockTestCaseResult()
        };

        var group = new TestCaseComplexityGroup(methodName, testCaseResults);

        // Act
        var (name, results) = group;

        // Assert
        name.ShouldBe(methodName);
        results.ShouldBe(testCaseResults);
        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithExpression_CreatesNewInstanceWithModifiedProperty()
    {
        // Arrange
        var testCaseResults = new List<ICompiledTestCaseResult> { CreateMockTestCaseResult() };
        var original = new TestCaseComplexityGroup("Original", testCaseResults);

        // Act
        var modified = original with { TestCaseMethodName = "Modified" };

        // Assert
        modified.TestCaseMethodName.ShouldBe("Modified");
        modified.TestCaseGroup.ShouldBe(original.TestCaseGroup);
        original.TestCaseMethodName.ShouldBe("Original");
    }

    [Fact]
    public void TestCaseGroup_CanBeModified()
    {
        // Arrange
        var testCaseResults = new List<ICompiledTestCaseResult> { CreateMockTestCaseResult() };
        var group = new TestCaseComplexityGroup("Test", testCaseResults);

        // Act
        group.TestCaseGroup.Add(CreateMockTestCaseResult());

        // Assert
        group.TestCaseGroup.Count.ShouldBe(2);
    }

    [Fact]
    public void EmptyTestCaseGroup_IsAllowed()
    {
        // Arrange
        var emptyResults = new List<ICompiledTestCaseResult>();

        // Act
        var group = new TestCaseComplexityGroup("EmptyMethod", emptyResults);

        // Assert
        group.TestCaseMethodName.ShouldBe("EmptyMethod");
        group.TestCaseGroup.ShouldBeEmpty();
    }

    private static ICompiledTestCaseResult CreateMockTestCaseResult()
    {
        var mock = Substitute.For<ICompiledTestCaseResult>();
        return mock;
    }
}

