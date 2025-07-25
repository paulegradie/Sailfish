using System;
using System.Collections.Generic;
using System.Linq;
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

    // Constructor Tests
    [Fact]
    public void DefaultConstructor_ShouldCreateEmptyVariables()
    {
        // Arrange & Act
        var testCaseVariables = new TestCaseVariables();

        // Assert
        testCaseVariables.Variables.ShouldNotBeNull();
        testCaseVariables.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void StringConstructor_ShouldParseSimpleDisplayName()
    {
        // Arrange
        var displayName = "TestMethod(var1:10, var2:test)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(2);

        // Variables should be ordered by name ascending, then by value
        variables[0].Name.ShouldBe("var1");
        variables[0].Value.ShouldBe(10);
        variables[1].Name.ShouldBe("var2");
        variables[1].Value.ShouldBe("test");
    }

    [Fact]
    public void StringConstructor_ShouldParseIntegerValues()
    {
        // Arrange
        var displayName = "TestMethod(count:42)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables[0].Name.ShouldBe("count");
        variables[0].Value.ShouldBe(42);
        variables[0].Value.ShouldBeOfType<int>();
    }

    [Fact]
    public void StringConstructor_ShouldParseStringValues()
    {
        // Arrange
        var displayName = "TestMethod(name:hello)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables[0].Name.ShouldBe("name");
        variables[0].Value.ShouldBe("hello");
        variables[0].Value.ShouldBeOfType<string>();
    }

    [Fact]
    public void StringConstructor_ShouldHandleEmptyDisplayName()
    {
        // Arrange
        var displayName = "";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        testCaseVariables.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void StringConstructor_ShouldHandleDisplayNameWithoutParentheses()
    {
        // Arrange
        var displayName = "TestMethod";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        testCaseVariables.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void StringConstructor_ShouldHandleEmptyParentheses()
    {
        // Arrange
        var displayName = "TestMethod()";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        testCaseVariables.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void JsonConstructor_ShouldOrderVariablesByName()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("zebra", 1),
            new("alpha", 2),
            new("beta", 3)
        };

        // Act
        var testCaseVariables = new TestCaseVariables(variables);

        // Assert
        var orderedVariables = testCaseVariables.Variables.ToList();
        orderedVariables.Count.ShouldBe(3);
        orderedVariables[0].Name.ShouldBe("alpha");
        orderedVariables[1].Name.ShouldBe("beta");
        orderedVariables[2].Name.ShouldBe("zebra");
    }

    // GetVariableIndex Tests
    [Fact]
    public void GetVariableIndex_ShouldReturnCorrectVariableAtValidIndex()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("first", 1),
            new("second", 2),
            new("third", 3)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.GetVariableIndex(1);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("second");
        result.Value.ShouldBe(2);
    }

    [Fact]
    public void GetVariableIndex_ShouldReturnNullForInvalidIndex()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("first", 1)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.GetVariableIndex(5);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetVariableIndex_ShouldReturnNullForNegativeIndex()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("first", 1)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.GetVariableIndex(-1);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetVariableIndex_ShouldReturnNullForEmptyVariables()
    {
        // Arrange
        var testCaseVariables = new TestCaseVariables();

        // Act
        var result = testCaseVariables.GetVariableIndex(0);

        // Assert
        result.ShouldBeNull();
    }

    // GetVariableByName Tests
    [Fact]
    public void GetVariableByName_ShouldReturnCorrectVariable()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("first", 1),
            new("second", "test"),
            new("third", true)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.GetVariableByName("second");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("second");
        result.Value.ShouldBe("test");
    }

    [Fact]
    public void GetVariableByName_ShouldThrowForNonExistentVariable()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("first", 1)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => testCaseVariables.GetVariableByName("nonexistent"));
    }

    [Fact]
    public void GetVariableByName_ShouldBeCaseSensitive()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("TestVar", 1)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => testCaseVariables.GetVariableByName("testvar"));
    }

    [Fact]
    public void GetVariableByName_ShouldThrowForEmptyVariables()
    {
        // Arrange
        var testCaseVariables = new TestCaseVariables();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => testCaseVariables.GetVariableByName("any"));
    }

    // FormVariableSection Edge Cases
    [Fact]
    public void FormVariableSection_ShouldEscapeCommasInValues()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("CommaVar", "value,with,commas")
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldContain("value\\,with\\,commas");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleWhitespaceInNamesAndValues()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("  SpacedName  ", "  spaced value  ")
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldContain("SpacedName");
        result.ShouldContain("spaced value");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleNullVariableValue()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("NullVar", null!),
            new("RegularVar", "test")
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("NullVar: ");
        result.ShouldContain("RegularVar: test");
        result.ShouldStartWith("(");
        result.ShouldEndWith(")");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleMultipleNullValues()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("FirstNull", null!),
            new("SecondNull", null!),
            new("NotNull", 42)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("FirstNull: ");
        result.ShouldContain("SecondNull: ");
        result.ShouldContain("NotNull: 42");
        // Should not throw any exceptions
    }

    [Fact]
    public void FormVariableSection_ShouldHandleOnlyNullValues()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("OnlyNull", null!)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act & Assert - Should not throw
        var result = testCaseVariables.FormVariableSection();

        result.ShouldNotBeNull();
        result.ShouldBe("(OnlyNull: )");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleDateTimeTypes()
    {
        // Arrange
        var dateTime = new DateTime(2023, 12, 25, 10, 30, 0);
        var timeSpan = TimeSpan.FromHours(2.5);
        var variables = new List<TestCaseVariable>
        {
            new("DateVar", dateTime),
            new("TimeVar", timeSpan)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("DateVar");
        result.ShouldContain("TimeVar");
    }

    [Fact]
    public void FormVariableSection_ShouldHandleEnumTypes()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("EnumVar", DayOfWeek.Monday)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("EnumVar");
        result.ShouldContain("Monday");
    }

    [Fact]
    public void FormVariableSection_ShouldCleanComplexObjectTypeNames()
    {
        // Arrange
        var complexObject = new ComplexObjectWithTypeName();
        var variables = new List<TestCaseVariable>
        {
            new("ComplexVar", complexObject)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("ComplexVar");
        // Should not contain the full type name in the output
        result.ShouldNotContain("ComplexObjectWithTypeName {");
    }

    private class ComplexObjectWithTypeName
    {
        public string Name { get; set; } = "Test";
        public int Value { get; set; } = 42;

        public override string ToString()
        {
            return $"ComplexObjectWithTypeName {{ Name = {Name}, Value = {Value} }}";
        }
    }

    // String Parsing Edge Cases
    [Fact]
    public void StringConstructor_ShouldHandleMultipleVariablesWithMixedTypes()
    {
        // Arrange
        var displayName = "TestMethod(intVar:123, stringVar:hello, boolVar:true)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(3);

        // Should be ordered by name ascending
        variables.ShouldContain(v => v.Name == "stringVar" && v.Value.Equals("hello"));
        variables.ShouldContain(v => v.Name == "intVar" && v.Value.Equals(123));
        variables.ShouldContain(v => v.Name == "boolVar" && v.Value.Equals("true"));
    }

    [Fact]
    public void StringConstructor_ShouldHandleVariablesWithColonsInValues()
    {
        // Arrange
        var displayName = "TestMethod(urlVar:http://example.com:8080)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables[0].Name.ShouldBe("urlVar");
        variables[0].Value.ShouldBe("http://example.com:8080");
    }

    [Fact]
    public void StringConstructor_ShouldHandleNegativeNumbers()
    {
        // Arrange
        var displayName = "TestMethod(negativeVar:-42)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables[0].Name.ShouldBe("negativeVar");
        variables[0].Value.ShouldBe(-42);
        variables[0].Value.ShouldBeOfType<int>();
    }

    [Fact]
    public void StringConstructor_ShouldHandleZeroValues()
    {
        // Arrange
        var displayName = "TestMethod(zeroVar:0)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables[0].Name.ShouldBe("zeroVar");
        variables[0].Value.ShouldBe(0);
        variables[0].Value.ShouldBeOfType<int>();
    }

    [Fact]
    public void StringConstructor_ShouldHandleEmptyStringValues()
    {
        // Arrange
        var displayName = "TestMethod(emptyVar:)";

        // Act
        var testCaseVariables = new TestCaseVariables(displayName);

        // Assert
        var variables = testCaseVariables.Variables.ToList();
        variables.Count.ShouldBe(1);
        variables[0].Name.ShouldBe("emptyVar");
        variables[0].Value.ShouldBe("");
    }

    // Variables Property Tests
    [Fact]
    public void Variables_ShouldBeImmutableReference()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("test", 1)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var retrievedVariables1 = testCaseVariables.Variables;
        var retrievedVariables2 = testCaseVariables.Variables;

        // Assert
        retrievedVariables1.ShouldBeSameAs(retrievedVariables2);
    }

    [Fact]
    public void Variables_ShouldMaintainOrdering()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("charlie", 3),
            new("alpha", 1),
            new("bravo", 2)
        };

        // Act
        var testCaseVariables = new TestCaseVariables(variables);

        // Assert
        var orderedVariables = testCaseVariables.Variables.ToList();
        orderedVariables[0].Name.ShouldBe("alpha");
        orderedVariables[1].Name.ShouldBe("bravo");
        orderedVariables[2].Name.ShouldBe("charlie");
    }

    // Complex Object Cleaning Tests
    [Fact]
    public void FormVariableSection_ShouldHandleObjectsWithNestedTypeNames()
    {
        // Arrange
        var nestedObject = new ObjectWithNestedTypes
        {
            SimpleProperty = "test",
            ComplexProperty = new ComplexTestObject { Name = "nested", Value = 99 }
        };
        var variables = new List<TestCaseVariable>
        {
            new("NestedObj", nestedObject)
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("NestedObj");
    }

    [Fact]
    public void FormVariableSection_ShouldPreserveBracketStructure()
    {
        // Arrange
        var variables = new List<TestCaseVariable>
        {
            new("var1", "value1"),
            new("var2", "value2")
        };
        var testCaseVariables = new TestCaseVariables(variables);

        // Act
        var result = testCaseVariables.FormVariableSection();

        // Assert
        result.ShouldStartWith("(");
        result.ShouldEndWith(")");
        result.ShouldContain("var1: value1");
        result.ShouldContain("var2: value2");
        result.ShouldContain(", ");
    }

    private class ObjectWithNestedTypes
    {
        public string SimpleProperty { get; set; } = string.Empty;
        public ComplexTestObject? ComplexProperty { get; set; }

        public override string ToString()
        {
            return $"ObjectWithNestedTypes {{ SimpleProperty = {SimpleProperty}, ComplexProperty = ComplexTestObject {{ Name = {ComplexProperty?.Name}, Value = {ComplexProperty?.Value} }} }}";
        }
    }
}
