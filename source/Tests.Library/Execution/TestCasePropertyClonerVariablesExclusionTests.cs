using System;
using System.Linq;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Utils;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class TestCasePropertyClonerVariablesExclusionTests
{
    [Fact]
    public void RetrieveProperties_ShouldExclude_ClassBasedSailfishVariables()
    {
        // Arrange
        var source = new SubjectWithClassBasedVariable
        {
            Echo = "keep me",
            SizeVariant = new SailfishVariables<SizeVariantData, SizeVariantProvider> { Value = new SizeVariantData("Small", 1) }
        };

        // Act
        var snapshot = source.RetrievePropertiesAndFields();
        var clonedPropertyNames = snapshot.Properties.Keys.Select(k => k.Name).ToArray();

        // Assert
        clonedPropertyNames.ShouldContain(nameof(SubjectWithClassBasedVariable.Echo));
        clonedPropertyNames.ShouldNotContain(nameof(SubjectWithClassBasedVariable.SizeVariant));
    }

    [Fact]
    public void ApplySavedState_ShouldNotOverride_ClassBasedSailfishVariables()
    {
        // Arrange - take snapshot from first instance (first test case)
        var first = new SubjectWithClassBasedVariable
        {
            Echo = "persist",
            SizeVariant = new SailfishVariables<SizeVariantData, SizeVariantProvider> { Value = new SizeVariantData("Small", 1) }
        };
        var snapshot = first.RetrievePropertiesAndFields();

        // Create a new instance representing a different test case value
        var second = new SubjectWithClassBasedVariable
        {
            Echo = "initial",
            SizeVariant = new SailfishVariables<SizeVariantData, SizeVariantProvider> { Value = new SizeVariantData("Large", 2) }
        };

        // Act - apply saved state from first to second
        snapshot.ApplyPropertiesAndFieldsTo(second);

        // Assert - Echo should be overwritten, but SizeVariant must remain the second's value
        second.Echo.ShouldBe("persist");
        second.SizeVariant.Value.Name.ShouldBe("Large");
        second.SizeVariant.Value.Value.ShouldBe(2);
    }

    // Test subject with a class-based SailfishVariables<T, TProvider> property
    public class SubjectWithClassBasedVariable
    {
        public string? Echo { get; set; }
        public SailfishVariables<SizeVariantData, SizeVariantProvider> SizeVariant { get; set; } = new();
    }

    // Simple data record and provider for the variable
    public record SizeVariantData(string Name, int Value) : IComparable
    {
        public int CompareTo(object? obj)
        {
            return obj is SizeVariantData other
                ? (Name, Value).CompareTo((other.Name, other.Value))
                : 1;
        }
    }

    public class SizeVariantProvider : ISailfishVariablesProvider<SizeVariantData>
    {
        public System.Collections.Generic.IEnumerable<SizeVariantData> Variables()
        {
            return new[]
            {
                new SizeVariantData("Small", 1),
                new SizeVariantData("Large", 2)
            };
        }
    }
}

