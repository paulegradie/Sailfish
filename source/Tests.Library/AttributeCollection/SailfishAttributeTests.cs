using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using Sailfish.Exceptions;
using Shouldly;
using Xunit;

namespace Tests.Library.AttributeCollection;

public class SailfishAttributeTests
{
    [Fact]
    public void SailfishRangeVariableAttributePropertiesAreSet()
    {
        var atty = new SailfishRangeVariableAttribute(1, 3, 2);

        var result = atty.GetVariables().Cast<int>().ToList();
        result.ShouldBeEquivalentTo(new List<int>() { 1, 3, 5 });
    }

    [Fact]
    public void SailfishRangeVariableAttributeScaleFishPropertyIsSet()
    {
        var atty = new SailfishRangeVariableAttribute(true, 1, 3, 2);

        atty.IsScaleFishVariable().ShouldBeTrue();
    }

    [Fact]
    public void SailfishMethodAttributePropertiesAreSet()
    {
        var atty = new SailfishMethodAttribute()
        {
            Disabled = true,
            DisableComplexity = true,
            Order = 0,
            DisableOverheadEstimation = true
        };

        atty.Disabled.ShouldBeTrue();
        atty.DisableComplexity.ShouldBeTrue();
        atty.DisableOverheadEstimation.ShouldBeTrue();
        atty.Order.ShouldBe(0);
    }


    [Fact]
    public void SailfishVariableAttributePropertiesAreSet()
    {
        var atty = new SailfishVariableAttribute(1, 2, 3);
        atty.GetVariables().Cast<int>().ToList().ShouldBeEquivalentTo(new List<int>() { 1, 2, 3 });
    }

    [Fact]
    public void SailfishVariableAttributeEmptyParamsWillThrow()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute());
    }

    [Fact]
    public void SailfishVariableAttributeShouldThrowWhenScaleFishAndNLessThanThree()
    {
        Should.Throw<SailfishException>(() => new SailfishVariableAttribute(true, 1, 2));
    }
    
    [Fact]
    public void SailfishVariableAttributeCanBeConfiguredFromMethod()
    {
        var atty = new SailfishVariableAttribute(typeof(TestVariablesSupplier));
        atty.GetVariables().Cast<string>().ToList().ShouldBeEquivalentTo(new List<string> { "A", "B", "C" });
    }

    public class TestVariablesSupplier : ISailfishVariablesProvider<string>
    {
        public IEnumerable<string> Variables() => ["A", "B", "C"];
    }
}