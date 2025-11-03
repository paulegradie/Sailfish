using System;
using System.ComponentModel.DataAnnotations;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore;

public class AttributesTests
{
    #region RealAttribute Tests

    [Fact]
    public void RealAttribute_WithDefaultValues_ShouldSetMinMaxToDoubleRange()
    {
        // Act
        var attribute = new RealAttribute();

        // Assert
        attribute.ShouldBeOfType<RealAttribute>();
        attribute.ShouldBeAssignableTo<RangeAttribute>();
        attribute.Minimum.ShouldBe(-1.7976931348623157E+308);
        attribute.Maximum.ShouldBe(1.7976931348623157E+308);
    }

    [Fact]
    public void RealAttribute_WithCustomMinMax_ShouldSetValues()
    {
        // Arrange
        var min = -100.0;
        var max = 100.0;

        // Act
        var attribute = new RealAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    [Fact]
    public void RealAttribute_WithZeroRange_ShouldAccept()
    {
        // Arrange
        var value = 42.0;

        // Act
        var attribute = new RealAttribute(value, value);

        // Assert
        attribute.Minimum.ShouldBe(value);
        attribute.Maximum.ShouldBe(value);
    }

    [Fact]
    public void RealAttribute_WithNegativeRange_ShouldAccept()
    {
        // Arrange
        var min = -1000.0;
        var max = -100.0;

        // Act
        var attribute = new RealAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    [Fact]
    public void RealAttribute_WithPositiveRange_ShouldAccept()
    {
        // Arrange
        var min = 100.0;
        var max = 1000.0;

        // Act
        var attribute = new RealAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    [Fact]
    public void RealAttribute_ShouldHaveParameterAttributeUsage()
    {
        // Act
        var attributeUsage = typeof(RealAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        attributeUsage.ShouldNotBeEmpty();
        var usage = (AttributeUsageAttribute)attributeUsage[0];
        usage.ValidOn.ShouldBe(AttributeTargets.Parameter);
    }

    #endregion

    #region PositiveAttribute Tests

    [Fact]
    public void PositiveAttribute_WithDefaultValues_ShouldSetMinToSmallestPositive()
    {
        // Act
        var attribute = new PositiveAttribute();

        // Assert
        attribute.ShouldBeOfType<PositiveAttribute>();
        attribute.ShouldBeAssignableTo<RealAttribute>();
        attribute.Minimum.ShouldBe(5E-324); // Smallest positive double
        attribute.Maximum.ShouldBe(1.7976931348623157E+308); // Largest double
    }

    [Fact]
    public void PositiveAttribute_WithCustomMinMax_ShouldSetValues()
    {
        // Arrange
        var min = 1.0;
        var max = 100.0;

        // Act
        var attribute = new PositiveAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    [Fact]
    public void PositiveAttribute_WithZeroMinimum_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            new PositiveAttribute(0.0, 100.0));
        exception.ParamName.ShouldBe("minimum");
    }

    [Fact]
    public void PositiveAttribute_WithNegativeMinimum_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            new PositiveAttribute(-1.0, 100.0));
        exception.ParamName.ShouldBe("minimum");
    }

    [Fact]
    public void PositiveAttribute_WithVerySmallPositiveMinimum_ShouldAccept()
    {
        // Arrange
        var min = 1e-300;
        var max = 100.0;

        // Act
        var attribute = new PositiveAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    [Fact]
    public void PositiveAttribute_WithLargeRange_ShouldAccept()
    {
        // Arrange
        var min = 1.0;
        var max = 1e100;

        // Act
        var attribute = new PositiveAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    [Fact]
    public void PositiveAttribute_WithMinEqualToMax_ShouldAccept()
    {
        // Arrange
        var value = 42.0;

        // Act
        var attribute = new PositiveAttribute(value, value);

        // Assert
        attribute.Minimum.ShouldBe(value);
        attribute.Maximum.ShouldBe(value);
    }

    [Fact]
    public void PositiveAttribute_ShouldHaveParameterAttributeUsage()
    {
        // Act
        var attributeUsage = typeof(PositiveAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        attributeUsage.ShouldNotBeEmpty();
        var usage = (AttributeUsageAttribute)attributeUsage[0];
        usage.ValidOn.ShouldBe(AttributeTargets.Parameter);
    }

    [Fact]
    public void PositiveAttribute_ShouldBeSealed()
    {
        // Act
        var type = typeof(PositiveAttribute);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void PositiveAttribute_WithBarelyPositiveMinimum_ShouldAccept()
    {
        // Arrange
        var min = double.Epsilon; // Smallest positive value
        var max = 100.0;

        // Act
        var attribute = new PositiveAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    [Fact]
    public void PositiveAttribute_WithMaxDoubleValue_ShouldAccept()
    {
        // Arrange
        var min = 1.0;
        var max = double.MaxValue;

        // Act
        var attribute = new PositiveAttribute(min, max);

        // Assert
        attribute.Minimum.ShouldBe(min);
        attribute.Maximum.ShouldBe(max);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void RealAttribute_InheritsFromRangeAttribute()
    {
        // Act
        var attribute = new RealAttribute();

        // Assert
        attribute.ShouldBeAssignableTo<RangeAttribute>();
    }

    [Fact]
    public void PositiveAttribute_InheritsFromRealAttribute()
    {
        // Act
        var attribute = new PositiveAttribute();

        // Assert
        attribute.ShouldBeAssignableTo<RealAttribute>();
        attribute.ShouldBeAssignableTo<RangeAttribute>();
    }

    [Fact]
    public void RealAttribute_CanBeAppliedToParameter()
    {
        // This test verifies the attribute can be used in code
        // The actual usage would be: void Method([Real] double value)
        
        // Act
        var attribute = new RealAttribute(-10, 10);

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Minimum.ShouldBe(-10);
        attribute.Maximum.ShouldBe(10);
    }

    [Fact]
    public void PositiveAttribute_CanBeAppliedToParameter()
    {
        // This test verifies the attribute can be used in code
        // The actual usage would be: void Method([Positive] double value)
        
        // Act
        var attribute = new PositiveAttribute(1, 100);

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Minimum.ShouldBe(1);
        attribute.Maximum.ShouldBe(100);
    }

    #endregion
}

