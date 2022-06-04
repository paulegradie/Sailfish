using System;
using System.Linq;
using System.Reflection;
using Shouldly;
using VeerPerforma;
using VeerPerforma.Attributes;
using Xunit;

namespace Test.AttributeCollection;

public class WhenCollectingAttributes
{
    private static bool HasAttribute<TAttribute>(Type type) where TAttribute : Attribute
    {
        return type.GetCustomAttributes(typeof(TAttribute), false).Length > 0;
    }

    [Fact]
    public void AllAttributesCanBeFound()
    {
        var allTypesWithAttribute = Assembly
            .GetAssembly(typeof(WhenCollectingAttributes))!
            .GetTypes()
            .Where(t => HasAttribute<VeerPerformaAttribute>(t))
            .ToArray();

        allTypesWithAttribute.Length.ShouldBeGreaterThan(0);
        allTypesWithAttribute
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToArray()
            .ShouldBe(
                new[] { nameof(TestClassOne), nameof(TestClassTwo) }
                    .OrderBy(x => x));
    }

    [VeerPerforma]
    public class TestClassOne
    {
    }

    [VeerPerforma(3)]
    public class TestClassTwo
    {
    }
}