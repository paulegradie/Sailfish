using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Exceptions;
using Shouldly;
using Xunit;

namespace Tests.Library.AttributeCollection;

public class SailfishAttributeTests
{
    [Fact]
    public void SailfishRangeVariableAttributePropertiesAreSet()
    {
        var atty = new SailfishRangeVariableAttribute(1, 3, 2);

        var result = atty.GetVariables().Cast<int>().ToList();
        result.ShouldBeEquivalentTo(new List<int>() { 1, 3, 5 });
    }

    [Fact]
    public void SailfishRangeVariableAttributeScaleFishPropertyIsSet()
    {
        var atty = new SailfishRangeVariableAttribute(true, 1, 3, 2);

        atty.IsScaleFishVariable().ShouldBeTrue();
    }

    [Fact]
    public void SailfishMethodAttributePropertiesAreSet()
    {
        var atty = new SailfishMethodAttribute()
        {
            Disabled = true,
            DisableComplexity = true,
            Order = 0,
            DisableOverheadEstimation = true
        };

        atty.Disabled.ShouldBeTrue();
        atty.DisableComplexity.ShouldBeTrue();
        atty.DisableOverheadEstimation.ShouldBeTrue();
        atty.Order.ShouldBe(0);
    }


    [Fact]
    public void SailfishVariableAttributePropertiesAreSet()
    {
        var atty = new SailfishVariableAttribute(1, 2, 3);
        atty.GetVariables().Cast<int>().ToList().ShouldBeEquivalentTo(new List<int>() { 1, 2, 3 });
    }

    [Fact]
    public void SailfishVariableAttributeEmptyIntParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new int[0]));
    }

    [Fact]
    public void SailfishVariableAttributeEmptyStringParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new string[0]));
    }

    [Fact]
    public void SailfishVariableAttributeEmptyDoubleParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new double[0]));
    }

    [Fact]
    public void SailfishVariableAttributeShouldThrowWhenScaleFishAndNLessThanThree()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(true, 1, 2));
    }



    [Fact]
    public void SailfishVariableAttributeShouldAcceptHomogeneousTypes()
    {
        // These should not throw
        var intAttr = new SailfishVariableAttribute(1, 2, 3);
        var stringAttr = new SailfishVariableAttribute("a", "b", "c");
        var doubleAttr = new SailfishVariableAttribute(1.0, 2.0, 3.0);

        intAttr.GetVariables().Cast<int>().ToList().ShouldBeEquivalentTo(new List<int> { 1, 2, 3 });
        stringAttr.GetVariables().Cast<string>().ToList().ShouldBeEquivalentTo(new List<string> { "a", "b", "c" });
        doubleAttr.GetVariables().Cast<double>().ToList().ShouldBeEquivalentTo(new List<double> { 1.0, 2.0, 3.0 });
    }

    [Fact]
    public void SailfishVariableAttributeDecimalConstructorShouldWork()
    {
        // Arrange & Act
        var decimalAttr = new SailfishVariableAttribute(1.5m, 2.5m, 3.5m);

        // Assert
        decimalAttr.GetVariables().Cast<decimal>().ToList().ShouldBeEquivalentTo(new List<decimal> { 1.5m, 2.5m, 3.5m });
    }

    [Fact]
    public void SailfishVariableAttributeEmptyDecimalParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new decimal[0]));
    }

    [Fact]
    public void SailfishVariableAttributeScaleFishConstructorShouldValidateMinimumValues()
    {
        // Should not throw with 3 or more values
        var validAttr = new SailfishVariableAttribute(true, 1, 2, 3);
        validAttr.IsScaleFishVariable().ShouldBeTrue();

        // Should throw with less than 3 values
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(true, 1, 2));
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(true, 1));
    }

    [Fact]
    public void SailfishVariableAttributeScaleFishConstructorShouldAcceptExactlyThreeValues()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(true, 10, 20, 30);

        // Assert
        attr.IsScaleFishVariable().ShouldBeTrue();
        attr.GetVariables().Cast<int>().ToList().ShouldBeEquivalentTo(new List<int> { 10, 20, 30 });
    }

    [Fact]
    public void SailfishVariableAttributeScaleFishConstructorShouldAcceptMoreThanThreeValues()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(true, 10, 20, 30, 40, 50);

        // Assert
        attr.IsScaleFishVariable().ShouldBeTrue();
        attr.GetVariables().Cast<int>().ToList().ShouldBeEquivalentTo(new List<int> { 10, 20, 30, 40, 50 });
    }

    [Fact]
    public void SailfishVariableAttributeRegularConstructorShouldNotBeScaleFish()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(1, 2, 3);

        // Assert
        attr.IsScaleFishVariable().ShouldBeFalse();
    }

    // Float constructor tests
    [Fact]
    public void SailfishVariableAttributeFloatConstructorShouldWork()
    {
        // Arrange & Act
        var floatAttr = new SailfishVariableAttribute(1.5f, 2.5f, 3.5f);

        // Assert
        floatAttr.GetVariables().Cast<float>().ToList().ShouldBeEquivalentTo(new List<float> { 1.5f, 2.5f, 3.5f });
        floatAttr.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariableAttributeEmptyFloatParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new float[0]));
    }

    [Fact]
    public void SailfishVariableAttributeSingleFloatValueShouldWork()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(42.0f);

        // Assert
        attr.GetVariables().Cast<float>().ToList().ShouldBeEquivalentTo(new List<float> { 42.0f });
    }

    // Long constructor tests
    [Fact]
    public void SailfishVariableAttributeLongConstructorShouldWork()
    {
        // Arrange & Act
        var longAttr = new SailfishVariableAttribute(100L, 200L, 300L);

        // Assert
        longAttr.GetVariables().Cast<long>().ToList().ShouldBeEquivalentTo(new List<long> { 100L, 200L, 300L });
        longAttr.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariableAttributeEmptyLongParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new long[0]));
    }

    [Fact]
    public void SailfishVariableAttributeSingleLongValueShouldWork()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(999L);

        // Assert
        attr.GetVariables().Cast<long>().ToList().ShouldBeEquivalentTo(new List<long> { 999L });
    }

    // Bool constructor tests
    [Fact]
    public void SailfishVariableAttributeBoolConstructorShouldWork()
    {
        // Arrange & Act
        var boolAttr = new SailfishVariableAttribute(true, false, true);

        // Assert
        boolAttr.GetVariables().Cast<bool>().ToList().ShouldBeEquivalentTo(new List<bool> { true, false, true });
        boolAttr.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariableAttributeEmptyBoolParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new bool[0]));
    }

    [Fact]
    public void SailfishVariableAttributeSingleBoolValueShouldWork()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute([true]);

        // Assert
        attr.GetVariables().Cast<bool>().ToList().ShouldBeEquivalentTo(new List<bool> { true });
        attr.IsScaleFishVariable().ShouldBeFalse();
    }

    // Additional single value tests for existing types
    [Fact]
    public void SailfishVariableAttributeSingleIntValueShouldWork()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(42);

        // Assert
        attr.GetVariables().Cast<int>().ToList().ShouldBeEquivalentTo(new List<int> { 42 });
        attr.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariableAttributeSingleStringValueShouldWork()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute("test");

        // Assert
        attr.GetVariables().Cast<string>().ToList().ShouldBeEquivalentTo(new List<string> { "test" });
        attr.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariableAttributeSingleDoubleValueShouldWork()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(3.14);

        // Assert
        attr.GetVariables().Cast<double>().ToList().ShouldBeEquivalentTo(new List<double> { 3.14 });
        attr.IsScaleFishVariable().ShouldBeFalse();
    }

    [Fact]
    public void SailfishVariableAttributeSingleDecimalValueShouldWork()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(2.5m);

        // Assert
        attr.GetVariables().Cast<decimal>().ToList().ShouldBeEquivalentTo(new List<decimal> { 2.5m });
        attr.IsScaleFishVariable().ShouldBeFalse();
    }

    // Interface implementation tests
    [Fact]
    public void SailfishVariableAttributeImplementsISailfishVariableAttribute()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(1, 2, 3);

        // Assert
        attr.ShouldBeAssignableTo<ISailfishVariableAttribute>();
    }

    [Fact]
    public void SailfishVariableAttributeGetVariablesReturnsCorrectType()
    {
        // Arrange & Act
        var attr = new SailfishVariableAttribute(1, 2, 3);
        var variables = attr.GetVariables();

        // Assert
        variables.ShouldNotBeNull();
        variables.ShouldBeOfType<object[]>();
        variables.Count().ShouldBe(3);
    }

    // Exception message tests
    [Fact]
    public void SailfishVariableAttributeEmptyArrayExceptionShouldHaveCorrectMessage()
    {
        // Arrange & Act & Assert
        var exception = Should.Throw<SailfishException>(() => new SailfishVariableAttribute(new int[0]));
        exception.Message.ShouldContain("No values were provided to the SailfishVariableAttribute attribute.");
    }

    [Fact]
    public void SailfishVariableAttributeScaleFishExceptionShouldHaveCorrectMessage()
    {
        // Arrange & Act & Assert
        var exception = Should.Throw<SailfishException>(() => new SailfishVariableAttribute(true, 1, 2));
        exception.Message.ShouldContain("Complexity estimation requires at least 3 variable values for n");
    }
}