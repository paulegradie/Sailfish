using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class WhenUsingSailfishVariablesClass
{
    [Fact]
    public void IterationVariableRetriever_ShouldDiscoverSailfishVariablesClassProperties()
    {
        // Arrange
        var retriever = new IterationVariableRetriever();
        var type = typeof(SailfishVariablesClassTestClass);

        // Act
        var variables = retriever.RetrieveIterationVariables(type);

        // Assert
        variables.Count.ShouldBe(2);
        variables.ShouldContainKey("SimpleValue");
        variables.ShouldContainKey("ClassBasedValue");

        // Check simple attribute variable
        var simpleVar = variables["SimpleValue"];
        simpleVar.OrderedVariables.Count().ShouldBe(3);
        simpleVar.OrderedVariables.ShouldBe(new object[] { 1, 2, 3 });

        // Check class-based variable
        var classVar = variables["ClassBasedValue"];
        classVar.OrderedVariables.Count().ShouldBe(2);
        classVar.OrderedVariables.ShouldAllBe(v => v is SailfishVariables<TestData, TestDataProvider>);
    }

    [Fact]
    public void AttributeDiscoveryExtensions_ShouldDetectSailfishVariablesClassProperties()
    {
        // Arrange
        var type = typeof(SailfishVariablesClassTestClass);

        // Act
        var classProperties = type.CollectAllSailfishVariablesClassProperties();
        var allProperties = type.CollectAllVariableProperties();

        // Assert
        classProperties.Count.ShouldBe(1);
        classProperties.Single().Name.ShouldBe("ClassBasedValue");

        allProperties.Count.ShouldBe(2);
        allProperties.ShouldContain(p => p.Name == "SimpleValue");
        allProperties.ShouldContain(p => p.Name == "ClassBasedValue");
    }

    [Fact]
    public void SailfishVariablesClassProvider_ShouldGenerateCorrectVariables()
    {
        // Arrange
        var propertyType = typeof(SailfishVariables<TestData, TestDataProvider>);
        var provider = new SailfishVariablesClassProvider(propertyType);

        // Act
        var variables = provider.GetVariables().ToArray();

        // Assert
        variables.Length.ShouldBe(2);
        variables.ShouldAllBe(v => v is SailfishVariables<TestData, TestDataProvider>);

        var firstVar = (SailfishVariables<TestData, TestDataProvider>)variables[0];
        var secondVar = (SailfishVariables<TestData, TestDataProvider>)variables[1];

        firstVar.Value.Name.ShouldBe("Config1");
        firstVar.Value.Value.ShouldBe(10);

        secondVar.Value.Name.ShouldBe("Config2");
        secondVar.Value.Value.ShouldBe(20);
    }

    [Fact]
    public void SailfishVariables_ShouldSupportImplicitConversion()
    {
        // Arrange
        var testData = new TestData("Test", 42);
        var sailfishVar = new SailfishVariables<TestData, TestDataProvider> { Value = testData };

        // Act & Assert - implicit conversion to TestData
        TestData converted = sailfishVar;
        converted.ShouldBe(testData);

        // Act & Assert - implicit conversion from TestData
        SailfishVariables<TestData, TestDataProvider> fromData = testData;
        fromData.Value.ShouldBe(testData);
    }

    [Fact]
    public void SailfishVariables_ShouldSupportComparison()
    {
        // Arrange
        var data1 = new TestData("A", 1);
        var data2 = new TestData("B", 2);
        var var1 = new SailfishVariables<TestData, TestDataProvider> { Value = data1 };
        var var2 = new SailfishVariables<TestData, TestDataProvider> { Value = data2 };

        // Act & Assert
        var1.CompareTo(var2).ShouldBeLessThan(0);
        var2.CompareTo(var1).ShouldBeGreaterThan(0);
        var1.CompareTo(var1).ShouldBe(0);

        // Test comparison with direct value
        var1.CompareTo(data1).ShouldBe(0);
        var1.CompareTo(data2).ShouldBeLessThan(0);
    }

    [Fact]
    public void PropertyTypeExtensions_ShouldCorrectlyIdentifySailfishVariablesClass()
    {
        // Arrange
        var type = typeof(SailfishVariablesClassTestClass);
        var properties = type.GetProperties();

        var simpleProperty = properties.First(p => p.Name == "SimpleValue");
        var classProperty = properties.First(p => p.Name == "ClassBasedValue");

        // Act & Assert
        simpleProperty.IsSailfishVariablesClassProperty().ShouldBeFalse();
        classProperty.IsSailfishVariablesClassProperty().ShouldBeTrue();

        simpleProperty.PropertyType.IsSailfishVariablesClass().ShouldBeFalse();
        classProperty.PropertyType.IsSailfishVariablesClass().ShouldBeTrue();
    }
}

// Test class with SailfishVariables<T, TProvider> property
public class SailfishVariablesClassTestClass
{
    [SailfishVariable(1, 2, 3)]
    public int SimpleValue { get; set; }

    public SailfishVariables<TestData, TestDataProvider> ClassBasedValue { get; set; } = new();
}

// Test data type
public record TestData(string Name, int Value) : IComparable
{
    public int CompareTo(object? obj)
    {
        if (obj is TestData other)
        {
            var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
            return nameComparison != 0 ? nameComparison : Value.CompareTo(other.Value);
        }
        return 1;
    }
}

// Test provider
public class TestDataProvider : ISailfishVariablesProvider<TestData>
{
    public IEnumerable<TestData> Variables()
    {
        return new[]
        {
            new TestData("Config1", 10),
            new TestData("Config2", 20)
        };
    }
}
