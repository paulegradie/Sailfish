using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class WhenUsingComplexVariableProviders
{


    [Fact]
    public void IterationVariableRetriever_ShouldHandleTypedVariables()
    {
        // Arrange
        var retriever = new IterationVariableRetriever();

        // Act
        var variables = retriever.RetrieveIterationVariables(typeof(MixedVariableTestClassWithTyped));

        // Assert
        variables.ShouldNotBeEmpty();
        variables.Count.ShouldBe(2);

        // Should have attribute-based and typed variables
        variables.ShouldContainKey("SimpleValue");
        variables.ShouldContainKey("TypedValue");

        // Check simple variable
        var simpleVar = variables["SimpleValue"];
        simpleVar.OrderedVariables.ShouldContain(1);
        simpleVar.OrderedVariables.ShouldContain(2);
        simpleVar.OrderedVariables.ShouldContain(3);

        // Check typed variable
        var typedVar = variables["TypedValue"];
        typedVar.OrderedVariables.Count().ShouldBe(2);
        typedVar.OrderedVariables.ShouldAllBe(v => v is TestTypedVariable);
    }



    [Fact]
    public void AttributeDiscoveryExtensions_ShouldDetectTypedVariableProperties()
    {
        // Arrange
        var type = typeof(MixedVariableTestClassWithTyped);

        // Act
        var typedProperties = type.CollectAllSailfishVariablesProperties();
        var allProperties = type.CollectAllVariableProperties();

        // Assert
        typedProperties.Count.ShouldBe(1);
        typedProperties.Single().Name.ShouldBe("TypedValue");

        allProperties.Count.ShouldBe(2);
        allProperties.ShouldContain(p => p.Name == "SimpleValue");
        allProperties.ShouldContain(p => p.Name == "TypedValue");
    }



    [Fact]
    public void TypedVariableProvider_ShouldReturnCorrectVariables()
    {
        // Arrange
        var provider = new TypedVariableProvider(typeof(ITestTypedVariable));

        // Act
        var variables = provider.GetVariables().ToList();

        // Assert
        variables.ShouldNotBeEmpty();
        variables.Count.ShouldBe(2);
        variables.ShouldAllBe(v => v is TestTypedVariable);

        var typedVariables = variables.Cast<TestTypedVariable>().ToList();
        typedVariables.ShouldContain(v => v.Name == "Config1" && v.Value == 10);
        typedVariables.ShouldContain(v => v.Name == "Config2" && v.Value == 20);
    }

    [Fact]
    public void TypeExtensions_ShouldCorrectlyIdentifyTypedVariables()
    {
        // Act & Assert
        typeof(TestTypedVariable).ImplementsISailfishVariables().ShouldBeTrue();
        typeof(string).ImplementsISailfishVariables().ShouldBeFalse();
        typeof(int).ImplementsISailfishVariables().ShouldBeFalse();
    }
}

// Test classes for the unit tests
public class MixedVariableTestClass
{
    [SailfishVariable(1, 2, 3)]
    public int SimpleValue { get; set; }
}

// Test class with all variable types
public class MixedVariableTestClassWithTyped
{
    [SailfishVariable(1, 2, 3)]
    public int SimpleValue { get; set; }

    public ITestTypedVariable TypedValue { get; set; } = null!;
}

// Test interface for typed variables
public interface ITestTypedVariable : ISailfishVariables<TestTypedVariable, TestTypedVariableProvider>
{
    string Name { get; }
    int Value { get; }
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

        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;

        return Value.CompareTo(other.Value);
    }
}
