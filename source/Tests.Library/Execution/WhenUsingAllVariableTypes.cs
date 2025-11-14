using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Execution;
using Shouldly;
using System;
using Xunit;

namespace Tests.Library.Execution;

public class WhenUsingAllVariableTypes
{
    [Fact]
    public void IterationVariableRetriever_ShouldHandleAllVariableTypes()
    {
        // Arrange
        var retriever = new IterationVariableRetriever();
        var type = typeof(AllVariableTypesTestClass);

        // Act
        var variables = retriever.RetrieveIterationVariables(type);

        // Assert
        variables.Count.ShouldBe(3);
        variables.ShouldContainKey("SimpleValue");
        variables.ShouldContainKey("TypedValue");
        variables.ShouldContainKey("RangeValue");

        // Check simple variable
        var simpleVar = variables["SimpleValue"];
        simpleVar.OrderedVariables.Count().ShouldBe(3);
        simpleVar.OrderedVariables.ShouldBe([1, 2, 3]);

        // Check typed variable
        var typedVar = variables["TypedValue"];
        typedVar.OrderedVariables.Count().ShouldBe(2);
        typedVar.OrderedVariables.ShouldAllBe(v => v is TestTypedVariable);

        // Check range variable
        var rangeVar = variables["RangeValue"];
        rangeVar.OrderedVariables.Count().ShouldBe(3);
        rangeVar.OrderedVariables.ShouldBe([10, 12, 14]);
    }

    [Fact]
    public void AttributeDiscoveryExtensions_ShouldDetectAllVariableTypes()
    {
        // Arrange
        var type = typeof(AllVariableTypesTestClass);

        // Act
        var attributeProperties = type.CollectAllSailfishVariableAttributes();
        var typedProperties = type.CollectAllSailfishVariablesProperties();
        var allProperties = type.CollectAllVariableProperties();

        // Assert
        attributeProperties.Count.ShouldBe(2); // SimpleValue and RangeValue
        typedProperties.Count.ShouldBe(1); // TypedValue
        allProperties.Count.ShouldBe(3); // SimpleValue, RangeValue, and TypedValue

        attributeProperties.ShouldContain(p => p.Name == "SimpleValue");
        attributeProperties.ShouldContain(p => p.Name == "RangeValue");
        typedProperties.Single().Name.ShouldBe("TypedValue");
    }

    [Fact]
    public void PropertyInfo_ShouldCorrectlyIdentifyAllVariableTypes()
    {
        // Arrange
        var type = typeof(AllVariableTypesTestClass);
        var simpleProperty = type.GetProperty("SimpleValue")!;
        var typedProperty = type.GetProperty("TypedValue")!;
        var rangeProperty = type.GetProperty("RangeValue")!;

        // Act & Assert
        simpleProperty.IsVariablesProperty().ShouldBeFalse();
        // Complex variable properties removed - simpleProperty.IsComplexVariableProperty().ShouldBeFalse();
        simpleProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();

        typedProperty.IsVariablesProperty().ShouldBeTrue();
        // Complex variable properties removed - typedProperty.IsComplexVariableProperty().ShouldBeFalse();
        typedProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();

        rangeProperty.IsVariablesProperty().ShouldBeFalse();
        // Complex variable properties removed - rangeProperty.IsComplexVariableProperty().ShouldBeFalse();
        rangeProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();
    }

    [Fact]
    public void IterationVariableRetriever_ShouldHandleClassWithNoVariables()
    {
        // Arrange
        var retriever = new IterationVariableRetriever();
        var type = typeof(NoVariablesTestClass);

        // Act
        var variables = retriever.RetrieveIterationVariables(type);

        // Assert
        variables.ShouldBeEmpty();
    }

    [Fact]
    public void IterationVariableRetriever_ShouldHandleNullType()
    {
        // Arrange
        var retriever = new IterationVariableRetriever();

        // Act & Assert
        // Should throw ArgumentNullException for proper null argument validation
        Should.Throw<ArgumentNullException>(() =>
            retriever.RetrieveIterationVariables(null!));
    }
}

// Test class with all variable types
public class AllVariableTypesTestClass
{
    [SailfishVariable(1, 2, 3)]
    public int SimpleValue { get; set; }

    public ITestTypedVariable TypedValue { get; set; } = null!;

    [SailfishRangeVariable(10, 3, 2)]
    public int RangeValue { get; set; }
}

// Test provider for typed variables
public class TestTypedVariableProvider : ISailfishVariablesProvider<TestTypedVariable>
{
    public IEnumerable<TestTypedVariable> Variables()
    {
        return
        [
            new TestTypedVariable("Config1", 10),
            new TestTypedVariable("Config2", 20)
        ];
    }
}

// Test data type for typed variables
public record TestTypedVariable : ITestTypedVariable
{
    public TestTypedVariable(string Name, int Value)
    {
        this.Name = Name;
        this.Value = Value;
    }

    public int CompareTo(object? obj)
    {
        if (obj is not TestTypedVariable other) return 1;

        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;

        return Value.CompareTo(other.Value);
    }

    public string Name { get; init; }
    public int Value { get; init; }

    public void Deconstruct(out string Name, out int Value)
    {
        Name = this.Name;
        Value = this.Value;
    }
}



// Test class with multiple variable types
public class MultipleVariableTypesTestClass
{
    [SailfishVariable(1, 2, 3)]
    public int IntProperty { get; set; }

    public ITestTypedVariable TypedProperty { get; set; } = null!;
}

// Test class with no variables
public class NoVariablesTestClass
{
    public string RegularProperty { get; set; } = string.Empty;

    public void RegularMethod()
    {
        // No Sailfish attributes
    }
}
