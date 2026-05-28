using System.Linq;
using Sailfish.Attributes;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies the geometric/linear spacing helpers on <see cref="SailfishRangeVariableAttribute"/>.
/// Geometric spacing is the recommended layout for ScaleFish complexity probes.
/// </summary>
public class ScaleFishLogSpacedRangeTests
{
    [Fact]
    public void LinearSpacing_EvenlySpaced()
    {
        var values = SailfishRangeVariableAttribute.SpacedRange(0, 100, 6, RangeSpacing.Linear).ToArray();
        values.ShouldBe(new[] { 0, 20, 40, 60, 80, 100 });
    }

    [Fact]
    public void GeometricSpacing_DoublingPattern()
    {
        // start=8, end=256 with 6 points should produce ~ 8, 16, 32, 64, 128, 256.
        var values = SailfishRangeVariableAttribute.SpacedRange(8, 256, 6, RangeSpacing.Geometric).ToArray();
        values.ShouldBe(new[] { 8, 16, 32, 64, 128, 256 });
    }

    [Fact]
    public void GeometricSpacing_AlwaysIncreasing()
    {
        var values = SailfishRangeVariableAttribute.SpacedRange(1, 10, 10, RangeSpacing.Geometric).ToArray();
        for (var i = 1; i < values.Length; i++)
            values[i].ShouldBeGreaterThan(values[i - 1]);
    }

    [Fact]
    public void GeometricSpacing_StartMustBePositive()
    {
        Should.Throw<System.ArgumentException>(
            () => SailfishRangeVariableAttribute.SpacedRange(0, 100, 5, RangeSpacing.Geometric).ToArray());
    }

    [Fact]
    public void GeometricSpacing_EndMustExceedStart()
    {
        Should.Throw<System.ArgumentException>(
            () => SailfishRangeVariableAttribute.SpacedRange(10, 10, 5, RangeSpacing.Linear).ToArray());
    }

    [Fact]
    public void GeometricSpacing_AttributeProducesVariablesAndEnablesScaleFish()
    {
        var attr = new SailfishRangeVariableAttribute(scaleFish: true, start: 4, end: 64, count: 5, spacing: RangeSpacing.Geometric);
        var values = attr.GetVariables().Cast<int>().ToArray();
        values.ShouldBe(new[] { 4, 8, 16, 32, 64 });
        attr.IsScaleFishVariable().ShouldBeTrue();
    }

    [Fact]
    public void GeometricSpacing_ScaleFishRequiresAtLeastThreePoints()
    {
        Should.Throw<Sailfish.Exceptions.SailfishException>(
            () => new SailfishRangeVariableAttribute(scaleFish: true, start: 4, end: 32, count: 2, spacing: RangeSpacing.Geometric));
    }
}
