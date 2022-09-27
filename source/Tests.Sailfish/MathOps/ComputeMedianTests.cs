using System;
using System.Linq;
using Sailfish.Utils.MathOps;
using Shouldly;
using Xunit;

namespace Test.MathOps;

public class ComputeMedianTests
{
    private readonly Random random = new();
    private readonly double[] evenValues;
    private readonly double[] oddValues;

    public ComputeMedianTests()
    {
        evenValues = Enumerable.Range(1, 100).OrderBy(val => random.Next()).Select(Convert.ToDouble).ToArray();
        oddValues = Enumerable.Range(1, 99).OrderBy(val => random.Next()).Select(Convert.ToDouble).ToArray();
    }

    [Fact]
    public void TestComputeMedianEven()
    {
        var result = Median.ComputeMedian(evenValues);
        result.ShouldBe(50.5);
    }

    [Fact]
    public void TestComputeMedianOdd()
    {
        var result = Median.ComputeMedian(oddValues);
        result.ShouldBe(50);
    }

    [Fact]
    public void TestComputeMedianIndexEven()
    {
        var result = Median.ComputeMedianIndex(evenValues);
        result.ShouldBe(50);
    }

    [Fact]
    public void TestComputeMedianIndexOdd()
    {
        var result = Median.ComputeMedianIndex(evenValues);
        result.ShouldBe(50);
    }
}