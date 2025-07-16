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
    public void ComplexVariableProvider_ShouldReturnCorrectVariables()
    {
        // Arrange
        var provider = new ComplexVariableProvider(typeof(TestComplexVariable));

        // Act
        var variables = provider.GetVariables().ToList();

        // Assert
        variables.ShouldNotBeEmpty();
        variables.Count.ShouldBe(3);
        variables.ShouldAllBe(v => v is TestComplexVariable);
        
        var typedVariables = variables.Cast<TestComplexVariable>().ToList();
        typedVariables.ShouldContain(v => v.Name == "Test1" && v.Value == 1);
        typedVariables.ShouldContain(v => v.Name == "Test2" && v.Value == 2);
        typedVariables.ShouldContain(v => v.Name == "Test3" && v.Value == 3);
    }

    [Fact]
    public void ComplexVariableProvider_ShouldNotBeScaleFishVariable()
    {
        // Arrange
        var provider = new ComplexVariableProvider(typeof(TestComplexVariable));

        // Act & Assert
        provider.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void IterationVariableRetriever_ShouldHandleBothAttributeAndComplexVariables()
    {
        // Arrange
        var retriever = new IterationVariableRetriever();

        // Act
        var variables = retriever.RetrieveIterationVariables(typeof(MixedVariableTestClass));

        // Assert
        variables.ShouldNotBeEmpty();
        variables.Count.ShouldBe(2);
        
        // Should have both attribute-based and complex variables
        variables.ShouldContainKey("SimpleValue");
        variables.ShouldContainKey("ComplexValue");
        
        // Check simple variable
        var simpleVar = variables["SimpleValue"];
        simpleVar.OrderedVariables.ShouldContain(1);
        simpleVar.OrderedVariables.ShouldContain(2);
        simpleVar.OrderedVariables.ShouldContain(3);
        
        // Check complex variable
        var complexVar = variables["ComplexValue"];
        complexVar.OrderedVariables.Count().ShouldBe(3);
        complexVar.OrderedVariables.ShouldAllBe(v => v is TestComplexVariable);
    }

    [Fact]
    public void AttributeDiscoveryExtensions_ShouldDetectComplexVariableProperties()
    {
        // Arrange
        var type = typeof(MixedVariableTestClass);

        // Act
        var complexProperties = type.CollectAllSailfishComplexVariableProperties();
        var allProperties = type.CollectAllVariableProperties();

        // Assert
        complexProperties.Count.ShouldBe(1);
        complexProperties.Single().Name.ShouldBe("ComplexValue");
        
        allProperties.Count.ShouldBe(2);
        allProperties.ShouldContain(p => p.Name == "SimpleValue");
        allProperties.ShouldContain(p => p.Name == "ComplexValue");
    }

    [Fact]
    public void TypeExtensions_ShouldCorrectlyIdentifyComplexVariableProvider()
    {
        // Act & Assert
        typeof(TestComplexVariable).ImplementsISailfishComplexVariableProvider().ShouldBeTrue();
        typeof(string).ImplementsISailfishComplexVariableProvider().ShouldBeFalse();
        typeof(int).ImplementsISailfishComplexVariableProvider().ShouldBeFalse();
    }
}

// Test classes for the unit tests
public class MixedVariableTestClass
{
    [SailfishVariable(1, 2, 3)]
    public int SimpleValue { get; set; }

    public ITestComplexVariable ComplexValue { get; set; } = null!;
}

public interface ITestComplexVariable : ISailfishComplexVariableProvider<TestComplexVariable>
{
    string Name { get; }
    int Value { get; }
}

public record TestComplexVariable(string Name, int Value) : ITestComplexVariable
{
    public int CompareTo(object? obj)
    {
        if (obj is not TestComplexVariable other) return 1;
        
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;
        
        return Value.CompareTo(other.Value);
    }

    public static IEnumerable<TestComplexVariable> GetVariableInstances()
    {
        return new[]
        {
            new TestComplexVariable("Test1", 1),
            new TestComplexVariable("Test2", 2),
            new TestComplexVariable("Test3", 3)
        };
    }
}
