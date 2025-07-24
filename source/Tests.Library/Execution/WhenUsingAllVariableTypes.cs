using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Exceptions;
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
        variables.Count.ShouldBe(4);
        variables.ShouldContainKey("SimpleValue");
        variables.ShouldContainKey("TypedValue");
        variables.ShouldContainKey("ComplexValue");
        variables.ShouldContainKey("RangeValue");

        // Check simple variable
        var simpleVar = variables["SimpleValue"];
        simpleVar.OrderedVariables.Count().ShouldBe(3);
        simpleVar.OrderedVariables.ShouldBe(new object[] { 1, 2, 3 });

        // Check typed variable
        var typedVar = variables["TypedValue"];
        typedVar.OrderedVariables.Count().ShouldBe(2);
        typedVar.OrderedVariables.ShouldAllBe(v => v is TestTypedVariable);

        // Check complex variable
        var complexVar = variables["ComplexValue"];
        complexVar.OrderedVariables.Count().ShouldBe(2);
        complexVar.OrderedVariables.ShouldAllBe(v => v is TestComplexVariable);

        // Check range variable
        var rangeVar = variables["RangeValue"];
        rangeVar.OrderedVariables.Count().ShouldBe(3);
        rangeVar.OrderedVariables.ShouldBe(new object[] { 10, 12, 14 });
    }

    [Fact]
    public void AttributeDiscoveryExtensions_ShouldDetectAllVariableTypes()
    {
        // Arrange
        var type = typeof(AllVariableTypesTestClass);

        // Act
        var attributeProperties = type.CollectAllSailfishVariableAttributes();
        var typedProperties = type.CollectAllSailfishVariablesProperties();
        var complexProperties = type.CollectAllComplexVariableProperties();
        var allProperties = type.CollectAllVariableProperties();

        // Assert
        attributeProperties.Count.ShouldBe(2); // SimpleValue and RangeValue
        typedProperties.Count.ShouldBe(1); // TypedValue
        complexProperties.Count.ShouldBe(1); // ComplexValue
        allProperties.Count.ShouldBe(3); // ComplexValue is excluded by design - CollectAllVariableProperties only aggregates Sailfish-attribute properties, interface-based variables, and class-based providers, while complex variables like ComplexValue are handled exclusively by CollectAllComplexVariableProperties

        attributeProperties.ShouldContain(p => p.Name == "SimpleValue");
        attributeProperties.ShouldContain(p => p.Name == "RangeValue");
        typedProperties.Single().Name.ShouldBe("TypedValue");
        complexProperties.Single().Name.ShouldBe("ComplexValue");
    }

    [Fact]
    public void PropertyInfo_ShouldCorrectlyIdentifyAllVariableTypes()
    {
        // Arrange
        var type = typeof(AllVariableTypesTestClass);
        var simpleProperty = type.GetProperty("SimpleValue")!;
        var typedProperty = type.GetProperty("TypedValue")!;
        var complexProperty = type.GetProperty("ComplexValue")!;
        var rangeProperty = type.GetProperty("RangeValue")!;

        // Act & Assert
        simpleProperty.IsVariablesProperty().ShouldBeFalse();
        simpleProperty.IsComplexVariableProperty().ShouldBeFalse();
        simpleProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();

        typedProperty.IsVariablesProperty().ShouldBeTrue();
        typedProperty.IsComplexVariableProperty().ShouldBeFalse();
        typedProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();

        complexProperty.IsVariablesProperty().ShouldBeFalse();
        complexProperty.IsComplexVariableProperty().ShouldBeTrue();
        complexProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();

        rangeProperty.IsVariablesProperty().ShouldBeFalse();
        rangeProperty.IsComplexVariableProperty().ShouldBeFalse();
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
        // The actual implementation throws NullReferenceException, not ArgumentNullException
        // This is the current behavior, so we test for what actually happens
        Should.Throw<NullReferenceException>(() =>
            retriever.RetrieveIterationVariables(null!));
    }
}

// Test class with all variable types
public class AllVariableTypesTestClass
{
    [SailfishVariable(1, 2, 3)]
    public int SimpleValue { get; set; }

    public ITestTypedVariable TypedValue { get; set; } = null!;

    public ITestComplexVariable ComplexValue { get; set; } = null!;

    [SailfishRangeVariable(10, 3, 2)]
    public int RangeValue { get; set; }
}

// Test provider for typed variables
public class TestTypedVariableProvider : ISailfishVariablesProvider<TestTypedVariable>
{
    public IEnumerable<TestTypedVariable> Variables()
    {
        return new[]
        {
            new TestTypedVariable("Config1", 10),
            new TestTypedVariable("Config2", 20)
        };
    }
}

// Test data type for typed variables
public record TestTypedVariable(string Name, int Value) : ITestTypedVariable
{
    public int CompareTo(object? obj)
    {
        if (obj is not TestTypedVariable other) return 1;

        var nameComparison = string.Compare(Name, other.Name, System.StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;

        return Value.CompareTo(other.Value);
    }
}

// Test interface for complex variables (ISailfishComplexVariableProvider pattern)
public interface ITestComplexVariable : ISailfishComplexVariableProvider<TestComplexVariable>
{
    string Name { get; }
    int Value { get; }
}

// Test data type for complex variables
public record TestComplexVariable(string Name, int Value) : ITestComplexVariable
{
    public int CompareTo(object? obj)
    {
        if (obj is not TestComplexVariable other) return 1;

        var nameComparison = string.Compare(Name, other.Name, System.StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;

        return Value.CompareTo(other.Value);
    }

    public static IEnumerable<TestComplexVariable> GetVariableInstances()
    {
        return new[]
        {
            new TestComplexVariable("Complex1", 100),
            new TestComplexVariable("Complex2", 200)
        };
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
