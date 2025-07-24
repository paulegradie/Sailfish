using System;
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
    public void IterationVariableRetriever_ShouldHandleMixedVariableTypes()
    {
        // Arrange
        var retriever = new IterationVariableRetriever();
        var type = typeof(MixedVariableTestClassWithTyped);

        // Act
        var variables = retriever.RetrieveIterationVariables(type);

        // Assert
        variables.Count.ShouldBe(2);
        variables.ShouldContainKey("SimpleValue");
        variables.ShouldContainKey("TypedValue");

        // Check simple variable
        var simpleVar = variables["SimpleValue"];
        simpleVar.OrderedVariables.Count().ShouldBe(3);
        simpleVar.OrderedVariables.ShouldBe(new object[] { 1, 2, 3 });

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
        typeof(ITestTypedVariable).ImplementsISailfishVariables().ShouldBeTrue();
        typeof(string).ImplementsISailfishVariables().ShouldBeFalse();
        typeof(int).ImplementsISailfishVariables().ShouldBeFalse();
    }

    [Fact]
    public void PropertyInfo_ShouldCorrectlyIdentifyVariableProperties()
    {
        // Arrange
        var type = typeof(MixedVariableTestClassWithTyped);
        var simpleProperty = type.GetProperty("SimpleValue")!;
        var typedProperty = type.GetProperty("TypedValue")!;

        // Act & Assert
        simpleProperty.IsVariablesProperty().ShouldBeFalse();
        typedProperty.IsVariablesProperty().ShouldBeTrue();

        simpleProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();
        typedProperty.HasAnySailfishVariableConfiguration().ShouldBeTrue();
    }

    [Fact]
    public void TypedVariableProvider_ShouldNotBeScaleFishVariable()
    {
        // Arrange
        var provider = new TypedVariableProvider(typeof(ITestTypedVariable));

        // Act & Assert
        provider.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void TypedVariableProvider_ShouldThrowOnNullType()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TypedVariableProvider(null!));
    }

    [Fact]
    public void AttributeVariableProvider_ShouldThrowOnNullAttribute()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new AttributeVariableProvider(null!));
    }

    [Fact]
    public void AttributeVariableProvider_ShouldReturnCorrectVariables()
    {
        // Arrange
        var attribute = new SailfishVariableAttribute(1, 2, 3);
        var provider = new AttributeVariableProvider(attribute);

        // Act
        var variables = provider.GetVariables().ToList();

        // Assert
        variables.ShouldNotBeEmpty();
        variables.Count.ShouldBe(3);
        variables.ToArray().ShouldBeEquivalentTo(new object[] { 1, 2, 3 });
    }

    [Fact]
    public void AttributeVariableProvider_ShouldReturnScaleFishStatus()
    {
        // Arrange
        var scaleFishAttribute = new SailfishVariableAttribute(true, 1, 2, 3);
        var regularAttribute = new SailfishVariableAttribute(1, 2, 3);
        var scaleFishProvider = new AttributeVariableProvider(scaleFishAttribute);
        var regularProvider = new AttributeVariableProvider(regularAttribute);

        // Act & Assert
        scaleFishProvider.IsScaleFishVariable().ShouldBeTrue();
        regularProvider.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariablesClassProvider_ShouldReturnCorrectVariables()
    {
        // Arrange
        var propertyType = typeof(SailfishVariables<TestTypedVariable, TestTypedVariableProvider>);
        var provider = new SailfishVariablesClassProvider(propertyType);

        // Act
        var variables = provider.GetVariables().ToArray();

        // Assert
        variables.Length.ShouldBe(2);
        variables.ShouldAllBe(v => v is SailfishVariables<TestTypedVariable, TestTypedVariableProvider>);
    }

    [Fact]
    public void SailfishVariablesClassProvider_ShouldNotBeScaleFishVariable()
    {
        // Arrange
        var propertyType = typeof(SailfishVariables<TestTypedVariable, TestTypedVariableProvider>);
        var provider = new SailfishVariablesClassProvider(propertyType);

        // Act & Assert
        provider.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariablesClassProvider_ShouldThrowOnNullType()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SailfishVariablesClassProvider(null!));
    }

    [Fact]
    public void ComplexVariableProvider_ShouldReturnCorrectVariables()
    {
        // Arrange
        var propertyType = typeof(ITestComplexVariable);
        var provider = new ComplexVariableProvider(propertyType);

        // Act
        var variables = provider.GetVariables().ToArray();

        // Assert
        variables.Length.ShouldBe(2);
        variables.ShouldAllBe(v => v is TestComplexVariable);
    }

    [Fact]
    public void ComplexVariableProvider_ShouldNotBeScaleFishVariable()
    {
        // Arrange
        var propertyType = typeof(ITestComplexVariable);
        var provider = new ComplexVariableProvider(propertyType);

        // Act & Assert
        provider.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void ComplexVariableProvider_ShouldThrowOnNullType()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ComplexVariableProvider(null!));
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

// Note: Test types are defined in WhenUsingAllVariableTypes.cs to avoid duplication
