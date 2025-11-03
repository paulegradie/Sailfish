using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore;

public class ExceptionsTests
{
    #region ConvergenceException Tests

    [Fact]
    public void ConvergenceException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Convergence failed after 100 iterations";

        // Act
        var exception = new ConvergenceException(message);

        // Assert
        exception.Message.ShouldBe(message);
        exception.ShouldBeOfType<ConvergenceException>();
        exception.ShouldBeAssignableTo<Exception>();
    }

    [Fact]
    public void ConvergenceException_WithNullMessage_ShouldAcceptNull()
    {
        // Act
        var exception = new ConvergenceException(null);

        // Assert
        exception.Message.ShouldNotBeNull(); // Base Exception class provides a default message
        exception.ShouldBeOfType<ConvergenceException>();
    }

    [Fact]
    public void ConvergenceException_WithEmptyMessage_ShouldSetEmptyMessage()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var exception = new ConvergenceException(message);

        // Assert
        exception.Message.ShouldBe(message);
    }

    [Fact]
    public void ConvergenceException_CanBeThrown()
    {
        // Arrange
        var message = "Test convergence exception";

        // Act & Assert
        var exception = Should.Throw<ConvergenceException>(() =>
        {
            throw new ConvergenceException(message);
        });
        exception.Message.ShouldBe(message);
    }

    [Fact]
    public void ConvergenceException_CanBeCaught()
    {
        // Arrange
        var message = "Test convergence exception";
        var caught = false;

        // Act
        try
        {
            throw new ConvergenceException(message);
        }
        catch (ConvergenceException ex)
        {
            caught = true;
            ex.Message.ShouldBe(message);
        }

        // Assert
        caught.ShouldBeTrue();
    }

    #endregion

    #region DimensionMismatchException Tests

    [Fact]
    public void DimensionMismatchException_WithParamName_ShouldSetMessageAndParamName()
    {
        // Arrange
        var paramName = "array1";

        // Act
        var exception = new DimensionMismatchException(paramName);

        // Assert
        exception.ParamName.ShouldBe(paramName);
        exception.Message.ShouldContain("Array dimensions must match");
        exception.ShouldBeOfType<DimensionMismatchException>();
        exception.ShouldBeAssignableTo<ArgumentException>();
    }

    [Fact]
    public void DimensionMismatchException_WithParamNameAndMessage_ShouldSetBoth()
    {
        // Arrange
        var paramName = "matrix";
        var message = "Expected 3x3 matrix but got 2x4";

        // Act
        var exception = new DimensionMismatchException(paramName, message);

        // Assert
        exception.ParamName.ShouldBe(paramName);
        exception.Message.ShouldContain(message);
    }

    [Fact]
    public void DimensionMismatchException_WithNullParamName_ShouldAcceptNull()
    {
        // Act
        var exception = new DimensionMismatchException(null!);

        // Assert
        exception.ParamName.ShouldBeNull();
        exception.Message.ShouldContain("Array dimensions must match");
    }

    [Fact]
    public void DimensionMismatchException_WithEmptyParamName_ShouldAcceptEmpty()
    {
        // Arrange
        var paramName = string.Empty;

        // Act
        var exception = new DimensionMismatchException(paramName);

        // Assert
        exception.ParamName.ShouldBe(paramName);
    }

    [Fact]
    public void DimensionMismatchException_CanBeThrown()
    {
        // Arrange
        var paramName = "testArray";

        // Act & Assert
        var exception = Should.Throw<DimensionMismatchException>(() =>
        {
            throw new DimensionMismatchException(paramName);
        });
        exception.ParamName.ShouldBe(paramName);
    }

    [Fact]
    public void DimensionMismatchException_CanBeCaught()
    {
        // Arrange
        var paramName = "testParam";
        var caught = false;

        // Act
        try
        {
            throw new DimensionMismatchException(paramName);
        }
        catch (DimensionMismatchException ex)
        {
            caught = true;
            ex.ParamName.ShouldBe(paramName);
        }

        // Assert
        caught.ShouldBeTrue();
    }

    [Fact]
    public void DimensionMismatchException_CanBeCaughtAsArgumentException()
    {
        // Arrange
        var paramName = "testParam";
        var caught = false;

        // Act
        try
        {
            throw new DimensionMismatchException(paramName);
        }
        catch (ArgumentException ex)
        {
            caught = true;
            ex.ParamName.ShouldBe(paramName);
        }

        // Assert
        caught.ShouldBeTrue();
    }

    [Fact]
    public void DimensionMismatchException_WithCustomMessage_ShouldPreserveCustomMessage()
    {
        // Arrange
        var paramName = "dimensions";
        var customMessage = "Matrix A has dimensions 3x4 but Matrix B has dimensions 2x5";

        // Act
        var exception = new DimensionMismatchException(paramName, customMessage);

        // Assert
        exception.Message.ShouldContain(customMessage);
        exception.ParamName.ShouldBe(paramName);
    }

    [Fact]
    public void DimensionMismatchException_WithNullCustomMessage_ShouldAcceptNull()
    {
        // Arrange
        var paramName = "testParam";

        // Act
        var exception = new DimensionMismatchException(paramName, null!);

        // Assert
        exception.ParamName.ShouldBe(paramName);
        exception.Message.ShouldNotBeNull();
    }

    [Fact]
    public void DimensionMismatchException_WithEmptyCustomMessage_ShouldAcceptEmpty()
    {
        // Arrange
        var paramName = "testParam";
        var message = string.Empty;

        // Act
        var exception = new DimensionMismatchException(paramName, message);

        // Assert
        exception.ParamName.ShouldBe(paramName);
    }

    #endregion
}

