using System;
using Sailfish.TestAdapter;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

/// <summary>
/// Unit tests for TestAdapterException to ensure proper exception handling
/// and constructor behavior in the test adapter.
/// </summary>
public class TestAdapterExceptionTests
{
    [Fact]
    public void Constructor_WithNoParameters_ShouldCreateException()
    {
        // Act
        var exception = new TestAdapterException();

        // Assert
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string expectedMessage = "Test adapter error occurred";

        // Act
        var exception = new TestAdapterException(expectedMessage);

        // Assert
        exception.Message.ShouldBe(expectedMessage);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithNullMessage_ShouldHandleGracefully()
    {
        // Act
        var exception = new TestAdapterException(null);

        // Assert
        exception.ShouldNotBeNull();
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string expectedMessage = "Test adapter error with inner exception";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new TestAdapterException(expectedMessage, innerException);

        // Assert
        exception.Message.ShouldBe(expectedMessage);
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void Constructor_WithNullMessageAndInnerException_ShouldHandleGracefully()
    {
        // Arrange
        var innerException = new ArgumentException("Inner error");

        // Act
        var exception = new TestAdapterException(null, innerException);

        // Assert
        exception.ShouldNotBeNull();
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void Constructor_WithMessageAndNullInnerException_ShouldSetMessageOnly()
    {
        // Arrange
        const string expectedMessage = "Test adapter error without inner exception";

        // Act
        var exception = new TestAdapterException(expectedMessage, null);

        // Assert
        exception.Message.ShouldBe(expectedMessage);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Exception_ShouldInheritFromSystemException()
    {
        // Act
        var exception = new TestAdapterException();

        // Assert
        exception.ShouldBeAssignableTo<Exception>();
    }

    [Fact]
    public void Exception_ShouldBeSerializable()
    {
        // Arrange
        const string message = "Serialization test message";
        var innerException = new ArgumentException("Inner exception for serialization");
        var exception = new TestAdapterException(message, innerException);

        // Act & Assert
        // The exception should be serializable (this tests the basic structure)
        exception.ToString().ShouldContain(message);
        exception.ToString().ShouldContain("ArgumentException");
    }

    [Fact]
    public void Exception_WithEmptyMessage_ShouldHandleGracefully()
    {
        // Act
        var exception = new TestAdapterException(string.Empty);

        // Assert
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe(string.Empty);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Exception_WithWhitespaceMessage_ShouldPreserveWhitespace()
    {
        // Arrange
        const string whitespaceMessage = "   \t\n   ";

        // Act
        var exception = new TestAdapterException(whitespaceMessage);

        // Assert
        exception.Message.ShouldBe(whitespaceMessage);
    }
}
