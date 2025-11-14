using System;
using System.Linq;
using Sailfish.Exceptions;
using Sailfish.Utils;
using Shouldly;
using Xunit;

namespace Tests.Library.Utils;

/// <summary>
/// Comprehensive unit tests for DisplayNameHelper.
/// Tests test case ID creation, display name formatting, and fully qualified name generation.
/// </summary>
public class DisplayNameHelperTests
{
    [Fact]
    public void CreateTestCaseId_WithNoVariables_ShouldCreateValidTestCaseId()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = Array.Empty<string>();
        var paramSet = Array.Empty<object>();

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.TestCaseName.Name.ShouldBe($"{testType.Name}.{methodName}");
        result.TestCaseVariables.Variables.ShouldBeEmpty();
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}()");
    }

    [Fact]
    public void CreateTestCaseId_WithSingleVariable_ShouldCreateValidTestCaseId()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "Size" };
        var paramSet = new object[] { 100 };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.TestCaseName.Name.ShouldBe($"{testType.Name}.{methodName}");
        result.TestCaseVariables.Variables.Count().ShouldBe(1);
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}(Size: 100)");
    }

    [Fact]
    public void CreateTestCaseId_WithMultipleVariables_ShouldCreateValidTestCaseId()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "Size", "Iterations" };
        var paramSet = new object[] { 100, 50 };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.TestCaseName.Name.ShouldBe($"{testType.Name}.{methodName}");
        result.TestCaseVariables.Variables.Count().ShouldBe(2);
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}(Iterations: 50, Size: 100)"); // Ordered alphabetically
    }

    [Fact]
    public void CreateTestCaseId_WithMismatchedArrayLengths_ShouldThrowException()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "Size", "Iterations" };
        var paramSet = new object[] { 100 }; // Mismatched length

        // Act & Assert
        Should.Throw<SailfishException>(() => 
            DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet))
            .Message.ShouldContain("Number of variables and number of params does not match");
    }

    [Fact]
    public void CreateTestCaseId_WithVariablesInDifferentOrder_ShouldOrderAlphabetically()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "ZVariable", "AVariable", "MVariable" };
        var paramSet = new object[] { "Z", "A", "M" };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}(AVariable: A, MVariable: M, ZVariable: Z)");
    }

    [Fact]
    public void CreateTestCaseId_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "NullValue" };
        var paramSet = new object[] { null! };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldContain("NullValue:");
    }

    [Fact]
    public void CreateTestCaseId_WithComplexObjects_ShouldUseToString()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "ComplexObject" };
        var paramSet = new object[] { new ComplexObject { Value = 42 } };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldContain("ComplexObject:");
        result.DisplayName.ShouldContain("42");
    }

    [Fact]
    public void FullyQualifiedName_WithMethodWithoutParameters_ShouldReturnCorrectName()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = nameof(TestClass.MethodWithoutParameters);

        // Act
        var result = DisplayNameHelper.FullyQualifiedName(testType, methodName);

        // Assert
        result.ShouldBe($"{testType.Namespace}.{testType.Name}.{methodName}()");
    }

    [Fact]
    public void FullyQualifiedName_WithMethodWithParameters_ShouldIncludeParameterTypes()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = nameof(TestClass.MethodWithParameters);

        // Act
        var result = DisplayNameHelper.FullyQualifiedName(testType, methodName);

        // Assert
        result.ShouldBe($"{testType.Namespace}.{testType.Name}.{methodName}(Int32, String)");
    }

    [Fact]
    public void FullyQualifiedName_WithMethodWithComplexParameters_ShouldIncludeParameterTypes()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = nameof(TestClass.MethodWithComplexParameters);

        // Act
        var result = DisplayNameHelper.FullyQualifiedName(testType, methodName);

        // Assert
        result.ShouldBe($"{testType.Namespace}.{testType.Name}.{methodName}(ComplexObject, DateTime)");
    }

    [Fact]
    public void FullyQualifiedName_WithNonExistentMethod_ShouldThrowException()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "NonExistentMethod";

        // Act & Assert
        Should.Throw<SailfishException>(() => 
            DisplayNameHelper.FullyQualifiedName(testType, methodName))
            .Message.ShouldContain($"Method name: {methodName} was not found on type {testType.Name}");
    }

    [Fact]
    public void FullyQualifiedName_WithOverloadedMethods_ShouldReturnFirstMatch()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = nameof(TestClass.OverloadedMethod);

        // Act
        var result = DisplayNameHelper.FullyQualifiedName(testType, methodName);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain(methodName);
        // Should return one of the overloaded methods
    }

    [Fact]
    public void CreateTestCaseId_WithStringVariables_ShouldFormatCorrectly()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "StringValue" };
        var paramSet = new object[] { "TestString" };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}(StringValue: TestString)");
    }

    [Fact]
    public void CreateTestCaseId_WithNumericVariables_ShouldFormatCorrectly()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "IntValue", "DoubleValue" };
        var paramSet = new object[] { 42, 3.14 };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}(DoubleValue: 3.14, IntValue: 42)");
    }

    [Fact]
    public void CreateTestCaseId_WithBooleanVariables_ShouldFormatCorrectly()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "BoolValue" };
        var paramSet = new object[] { true };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}(BoolValue: True)");
    }

    [Fact]
    public void CreateTestCaseId_WithEmptyStringVariable_ShouldHandleGracefully()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = "TestMethod";
        var variableNames = new[] { "EmptyString" };
        var paramSet = new object[] { "" };

        // Act
        var result = DisplayNameHelper.CreateTestCaseId(testType, methodName, variableNames, paramSet);

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe($"{testType.Name}.{methodName}(EmptyString: )");
    }

    [Fact]
    public void FullyQualifiedName_WithGenericMethod_ShouldHandleCorrectly()
    {
        // Arrange
        var testType = typeof(TestClass);
        var methodName = nameof(TestClass.GenericMethod);

        // Act
        var result = DisplayNameHelper.FullyQualifiedName(testType, methodName);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain(methodName);
    }

    // Test classes and helper types
    private class TestClass
    {
        public void MethodWithoutParameters() { }
        
        public void MethodWithParameters(int value, string text) { }
        
        public void MethodWithComplexParameters(ComplexObject obj, DateTime date) { }
        
        public void OverloadedMethod() { }
        
        public void OverloadedMethod(int value) { }
        
        public void OverloadedMethod(string text) { }
        
        public void GenericMethod<T>(T value) { }
    }

    private class ComplexObject
    {
        public int Value { get; set; }
        
        public override string ToString()
        {
            return $"ComplexObject({Value})";
        }
    }
}
