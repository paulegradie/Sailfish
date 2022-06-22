using System.Linq;
using System.Reflection;
using Shouldly;
using Sailfish.Attributes;
using Xunit;
using Sailfish.Utils;

namespace Test.AttributeCollection
{
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
            allTypesWithAttribute
                .Select(x => x.Name)
                .OrderBy(x => x)
                .ToArray()
                .ShouldBe(
                    new[] {nameof(TestClassOne), nameof(TestClassTwo)}
                        .OrderBy(x => x));
        }

        [Sailfish]
        public class TestClassOne
        {
        }

        [Sailfish(3)]
        public class TestClassTwo
        {
        }
    }
}