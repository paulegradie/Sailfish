using Sailfish.Attributes;
using Sailfish.Extensions.Methods;
using Shouldly;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Tests.Library.AttributeCollection;

public class WhenCollectingAttributes
{
    [Fact]
    public void AllAttributesCanBeFound()
    {
        var allTypesWithAttribute = Assembly
            .GetAssembly(typeof(WhenCollectingAttributes))!
            .GetTypes()
            .Where(t => t.HasAttribute<SailfishAttribute>())
            .ToArray();

        allTypesWithAttribute.Length.ShouldBeGreaterThan(0);
        var result = allTypesWithAttribute
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToArray();

        result.ShouldContain(nameof(TestClassOne));
        result.ShouldContain(nameof(TestClassTwo));
        result.ShouldNotContain(nameof(TestClassThree));
    }

    [Sailfish]
    public class TestClassOne
    {
    }

    [Sailfish(3)]
    public class TestClassTwo
    {
    }

    public class TestClassThree
    {
    }
}