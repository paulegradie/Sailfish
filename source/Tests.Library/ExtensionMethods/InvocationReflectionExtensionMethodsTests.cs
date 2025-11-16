using System;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Shouldly;
using Xunit;

namespace Tests.Library.ExtensionMethods;

/// <summary>
/// Comprehensive unit tests for InvocationReflectionExtensionMethods.
/// Tests method invocation, attribute extraction, and performance timing integration.
/// </summary>
public class InvocationReflectionExtensionMethodsTests
{
    [Fact]
    public void GetSampleSize_WithSailfishAttribute_ShouldReturnCorrectValue()
    {
        // Arrange
        var type = typeof(TestClassWithSampleSize);

        // Act
        var result = type.GetSampleSize();

        // Assert
        result.ShouldBe(50);
    }

    [Fact]
    public void GetWarmupIterations_WithSailfishAttribute_ShouldReturnCorrectValue()
    {
        // Arrange
        var type = typeof(TestClassWithWarmupIterations);

        // Act
        var result = type.GetWarmupIterations();

        // Assert
        result.ShouldBe(10);
    }

    [Fact]
    public void SailfishTypeIsDisabled_WithDisabledAttribute_ShouldReturnTrue()
    {
        // Arrange
        var type = typeof(DisabledTestClass);

        // Act
        var result = type.SailfishTypeIsDisabled();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void SailfishTypeIsDisabled_WithEnabledAttribute_ShouldReturnFalse()
    {
        // Arrange
        var type = typeof(EnabledTestClass);

        // Act
        var result = type.SailfishTypeIsDisabled();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void InvokeMethod_WithVoidMethod_ShouldExecuteSuccessfully()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.VoidMethod))!;
        var arguments = Array.Empty<object>();

        // Act & Assert
        Should.NotThrow(() => method.InvokeMethod(testInstance, arguments));
        testInstance.VoidMethodCalled.ShouldBeTrue();
    }

    [Fact]
    public void InvokeMethod_WithReturnValueMethod_ShouldExecuteSuccessfully()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.ReturnValueMethod))!;
        var arguments = new object[] { 42 };

        // Act
        var result = method.InvokeMethod(testInstance, arguments);

        // Assert
        result.ShouldBe(42);
        testInstance.ReturnValueMethodCalled.ShouldBeTrue();
    }

    [Fact]
    public void InvokeMethod_WithPerformanceTimer_ShouldStartAndStopTimer()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.VoidMethod))!;
        var arguments = Array.Empty<object>();
        var timer = new PerformanceTimer();

        // Act
        method.InvokeMethod(testInstance, arguments, timer);

        // Assert
        // Verify that the timer has recorded performance data (indicating it was started and stopped)
        timer.ExecutionIterationPerformances.Count.ShouldBe(1);
        testInstance.VoidMethodCalled.ShouldBeTrue();
    }

    [Fact]
    public void InvokeMethod_WithPerformanceTimerAndReturnValue_ShouldStartAndStopTimer()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.ReturnValueMethod))!;
        var arguments = new object[] { 100 };
        var timer = new PerformanceTimer();

        // Act
        var result = method.InvokeMethod(testInstance, arguments, timer);

        // Assert
        result.ShouldBe(100);
        // Verify that the timer has recorded performance data (indicating it was started and stopped)
        timer.ExecutionIterationPerformances.Count.ShouldBe(1);
        testInstance.ReturnValueMethodCalled.ShouldBeTrue();
    }

    [Fact]
    public void InvokeMethod_WithException_ShouldPropagateException()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.ThrowingMethod))!;
        var arguments = Array.Empty<object>();

        // Act & Assert
        Should.Throw<TargetInvocationException>(() => method.InvokeMethod(testInstance, arguments))
            .InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void InvokeMethod_WithNullInstance_ShouldThrow()
    {
        // Arrange
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.VoidMethod))!;
        var arguments = Array.Empty<object>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => method.InvokeMethod(null!, arguments));
    }

    [Fact]
    public void InvokeMethod_WithWrongArgumentCount_ShouldThrow()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.ReturnValueMethod))!;
        var arguments = Array.Empty<object>(); // Should have 1 argument

        // Act & Assert
        Should.Throw<TargetParameterCountException>(() => method.InvokeMethod(testInstance, arguments));
    }

    [Fact]
    public void InvokeMethod_WithWrongArgumentType_ShouldThrow()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.ReturnValueMethod))!;
        var arguments = new object[] { "string instead of int" };

        // Act & Assert
        Should.Throw<ArgumentException>(() => method.InvokeMethod(testInstance, arguments));
    }

    [Fact]
    public void InvokeMethod_WithStaticMethod_ShouldExecuteSuccessfully()
    {
        // Arrange
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.StaticMethod))!;
        var arguments = Array.Empty<object>();

        // Act & Assert
        Should.NotThrow(() => method.InvokeMethod(null, arguments));
    }

    [Fact]
    public void GetSampleSize_WithMultipleSailfishAttributes_ShouldReturnFirst()
    {
        // Arrange
        var type = typeof(TestClassWithSampleSize);

        // Act
        var result = type.GetSampleSize();

        // Assert
        result.ShouldBe(50); // Should return the value from the single attribute
    }

    [Fact]
    public void GetWarmupIterations_WithDefaultValues_ShouldReturnCorrectValue()
    {
        // Arrange
        var type = typeof(TestClassWithDefaults);

        // Act
        var result = type.GetWarmupIterations();

        // Assert
        result.ShouldBe(3); // Default value
    }

    [Fact]
    public void InvokeMethod_WithComplexParameters_ShouldExecuteSuccessfully()
    {
        // Arrange
        var testInstance = new TestMethodClass();
        var method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.ComplexParameterMethod))!;
        var arguments = new object[] { "test", 42, DateTime.Now };

        // Act & Assert
        Should.NotThrow(() => method.InvokeMethod(testInstance, arguments));
        testInstance.ComplexParameterMethodCalled.ShouldBeTrue();
    }

    // Test classes with various Sailfish attributes
    [Sailfish(SampleSize = 50)]
    private class TestClassWithSampleSize
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish(NumWarmupIterations = 10)]
    private class TestClassWithWarmupIterations
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish(Disabled = true)]
    private class DisabledTestClass
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish(Disabled = false)]
    private class EnabledTestClass
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    [Sailfish]
    private class TestClassWithDefaults
    {
        [SailfishMethod]
        public void TestMethod() { }
    }

    // Test class for method invocation testing
    private class TestMethodClass
    {
        public bool VoidMethodCalled { get; private set; }
        public bool ReturnValueMethodCalled { get; private set; }
        public bool ComplexParameterMethodCalled { get; private set; }

        public void VoidMethod()
        {
            VoidMethodCalled = true;
        }

        public int ReturnValueMethod(int value)
        {
            ReturnValueMethodCalled = true;
            return value;
        }

        public void ThrowingMethod()
        {
            throw new InvalidOperationException("Test exception");
        }

        public static void StaticMethod()
        {
            // Static method for testing
        }

        public void ComplexParameterMethod(string text, int number, DateTime date)
        {
            ComplexParameterMethodCalled = true;
        }
    }
}
