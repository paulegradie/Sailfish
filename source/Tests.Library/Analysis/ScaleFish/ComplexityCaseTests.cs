using System.Collections.Generic;
using System.Reflection;
using Sailfish.Analysis.ScaleFish;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ComplexityCaseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var propertyName = "TestProperty";
        var propertyInfo = typeof(TestClass).GetProperty(nameof(TestClass.N))!;
        var variableCount = 5;
        var variables = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var complexityCase = new ComplexityCase(propertyName, propertyInfo, variableCount, variables);

        // Assert
        complexityCase.ComplexityPropertyName.ShouldBe(propertyName);
        complexityCase.ComplexityProperty.ShouldBe(propertyInfo);
        complexityCase.VariableCount.ShouldBe(variableCount);
        complexityCase.Variables.ShouldBe(variables);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var propertyInfo = typeof(TestClass).GetProperty(nameof(TestClass.N))!;
        var variables = new List<int> { 1, 2, 3 };

        var case1 = new ComplexityCase("Prop1", propertyInfo, 3, variables);
        var case2 = new ComplexityCase("Prop1", propertyInfo, 3, variables);
        var case3 = new ComplexityCase("Prop2", propertyInfo, 3, variables);

        // Act & Assert
        case1.ShouldBe(case2);
        case1.ShouldNotBe(case3);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        // Arrange
        var propertyName = "TestProperty";
        var propertyInfo = typeof(TestClass).GetProperty(nameof(TestClass.N))!;
        var variableCount = 3;
        var variables = new List<int> { 10, 20, 30 };

        var complexityCase = new ComplexityCase(propertyName, propertyInfo, variableCount, variables);

        // Act
        var (name, prop, count, vars) = complexityCase;

        // Assert
        name.ShouldBe(propertyName);
        prop.ShouldBe(propertyInfo);
        count.ShouldBe(variableCount);
        vars.ShouldBe(variables);
    }

    [Fact]
    public void WithExpression_CreatesNewInstanceWithModifiedProperty()
    {
        // Arrange
        var propertyInfo = typeof(TestClass).GetProperty(nameof(TestClass.N))!;
        var variables = new List<int> { 1, 2, 3 };
        var original = new ComplexityCase("Original", propertyInfo, 3, variables);

        // Act
        var modified = original with { ComplexityPropertyName = "Modified" };

        // Assert
        modified.ComplexityPropertyName.ShouldBe("Modified");
        modified.ComplexityProperty.ShouldBe(original.ComplexityProperty);
        modified.VariableCount.ShouldBe(original.VariableCount);
        modified.Variables.ShouldBe(original.Variables);
        original.ComplexityPropertyName.ShouldBe("Original");
    }

    [Fact]
    public void VariablesList_CanBeModified()
    {
        // Arrange
        var propertyInfo = typeof(TestClass).GetProperty(nameof(TestClass.N))!;
        var variables = new List<int> { 1, 2, 3 };
        var complexityCase = new ComplexityCase("Test", propertyInfo, 3, variables);

        // Act
        complexityCase.Variables.Add(4);

        // Assert
        complexityCase.Variables.Count.ShouldBe(4);
        complexityCase.Variables.ShouldContain(4);
    }

    private class TestClass
    {
        public int N { get; set; }
    }
}

