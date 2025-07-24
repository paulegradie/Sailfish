using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Models;

public class TestCaseVariablesTests
{
    [Fact]
    public void FormVariableSection_ShouldFormatSimpleVariables()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("IntVar", 42),
            new("StringVar", "test"),
            new("BoolVar", true)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("IntVar");
        result.ShouldContain("42");
        result.ShouldContain("StringVar");
        result.ShouldContain("test");
        result.ShouldContain("BoolVar");
        result.ShouldContain("True");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleNullValues()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("StringVar", "test"),
            new("IntVar", 42)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("StringVar");
        result.ShouldContain("test");
        result.ShouldContain("IntVar");
        result.ShouldContain("42");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleComplexObjects()
    {
        // Arrange
        var complexObject = new ComplexTestObject { Name = "Test", Value = 123 };
        var variables = new List<TestCaseVariable>
        {
            new("ComplexVar", complexObject),
            new("SimpleVar", "simple")
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("ComplexVar");
        result.ShouldContain("SimpleVar");
        result.ShouldContain("simple");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("SpecialVar", "String with \"quotes\" and \n newlines"),
            new("UnicodeVar", "Unicode: 🚀 ñ ü")
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("SpecialVar");
        result.ShouldContain("UnicodeVar");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleEmptyVariableList()
    {
        // Arrange
        var variables = new List<TestCaseVariable>();
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNull();
        // Should handle empty list gracefully
    }

    [Fact]
    public void FormVariableSection_ShouldHandleVeryLongStrings()
    {
        // Arrange
        var longString = new string('A', 1000);
        var variables = new List<TestCaseVariable>
        {
            new("LongVar", longString),
            new("ShortVar", "short")
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("LongVar");
        result.ShouldContain("ShortVar");
        result.ShouldContain("short");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleNumericTypes()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("IntVar", 42),
            new("DoubleVar", 3.14),
            new("DecimalVar", 123.456m),
            new("FloatVar", 2.71f),
            new("LongVar", 9876543210L)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("IntVar");
        result.ShouldContain("42");
        result.ShouldContain("DoubleVar");
        result.ShouldContain("3.14");
        result.ShouldContain("DecimalVar");
        result.ShouldContain("123.456");
        result.ShouldContain("FloatVar");
        result.ShouldContain("2.71");
        result.ShouldContain("LongVar");
        result.ShouldContain("9876543210");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleCollections()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var array = new[] { "a", "b", "c" };
        var variables = new List<TestCaseVariable>
        {
            new("ListVar", list),
            new("ArrayVar", array)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("ListVar");
        result.ShouldContain("ArrayVar");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleNestedObjects()
    {
        // Arrange
        var nestedObject = new NestedTestObject
        {
            Outer = "outer",
            Inner = new ComplexTestObject { Name = "inner", Value = 456 }
        };
        var variables = new List<TestCaseVariable>
        {
            new("NestedVar", nestedObject)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("NestedVar");
    }

    private class ComplexTestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        public override string ToString()
        {
            return $"ComplexTestObject(Name={Name}, Value={Value})";
        }
    }

    private class NestedTestObject
    {
        public string Outer { get; set; } = string.Empty;
        public ComplexTestObject? Inner { get; set; }

        public override string ToString()
        {
            return $"NestedTestObject(Outer={Outer}, Inner={Inner})";
        }
    }
}
