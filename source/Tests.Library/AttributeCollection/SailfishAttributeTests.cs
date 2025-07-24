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
}