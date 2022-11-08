using System;
using System.Linq;
using Sailfish.MathOps;
using Shouldly;
using Xunit;

namespace Test.MathOps;

public class ComputeInterQuartileTests
{
    readonly Random random = new();
    readonly double[] values;

    public ComputeInterQuartileTests()
    {
        values = Enumerable.Range(1, 100).OrderBy(val => random.Next()).Select(Convert.ToDouble).ToArray();
    }

    [Fact]
    public void TestGetInterQuartileBounds()
    {
        var result = ComputeQuartiles.GetInterQuartileBounds(values.ToArray());

        result[ComputeQuartiles.Lower].ShouldBe(26);
        result[ComputeQuartiles.Upper].ShouldBe(75);
    }

    [Fact]
    public void TestGetInterQuartileValues()
    {
        var result = ComputeQuartiles.GetInnerQuartileValues(values);

        var expected = Enumerable.Range(26, 50).Select(Convert.ToDouble).ToArray();
        result.ShouldBeEquivalentTo(expected);
        result.Length.ShouldBe(50);
    }

    [Fact]
    public void TestGetOuterQuartileValues()
    {
        var result = ComputeQuartiles.GetOuterQuartileValues(values);

        var expected = Enumerable
            .Range(1, 25)
            .Select(Convert.ToDouble)
            .Concat(
                Enumerable
                    .Range(76, 25)
                    .Select(Convert.ToDouble))
            .ToArray();
        result.ShouldBeEquivalentTo(expected);
    }
}