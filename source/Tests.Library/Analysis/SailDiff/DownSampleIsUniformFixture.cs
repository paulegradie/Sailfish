using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class DownSampleIsUniformFixture
{
    private const int Seed = 42;
    private readonly TestPreprocessor _preprocessor = new(new SailfishOutlierDetector());

    [Fact]
    public void TestDownSampleIsUniform()
    {
        var input = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var output = _preprocessor.PreprocessWithDownSample(input, false, maxArraySize: 10, seed: Seed);

        var expected = new double[] { 14, 3, 11, 4, 6, 15, 16, 5, 7, 8 };
        output.RawData.ShouldBe(expected);
    }

    [Fact]
    public void TestDownSampleIsUniformAlternate()
    {
        var input = new double[] { 14, 15, 532, 52, 534, 78, 47, 732, 226, 27, 277, 234, 620, 206, 342, 623, 66, 342, 26, 342 };
        var output = _preprocessor.PreprocessWithDownSample(input, false, maxArraySize: 10, seed: Seed);

        var expected = new double[] { 206, 532, 277, 52, 78, 342, 623, 534, 47, 732 };
        output.RawData.ShouldBe(expected);
    }

    [Fact]
    public void TestDownSampleSmallArray()
    {
        var input = new double[] { 1, 2, 3 };

        var output = _preprocessor.PreprocessWithDownSample(input, false, maxArraySize: 10, seed: Seed);

        output.RawData.Length.ShouldBe(3);
        input.ShouldBe(output.RawData);
    }

    [Fact]
    public void TestDownSampleMaxLessThanMin()
    {
        var input = new double[] { 1, 2, 3, 4, 5 };

        var output = _preprocessor.PreprocessWithDownSample(input, false, 5, 2, Seed);

        output.RawData.Length.ShouldBe(5);
        input.ShouldBe(output.RawData);
    }

    [Fact]
    public void TestDownSampleMaxEqualToMin()
    {
        var input = new double[] { 1, 2, 3, 4, 5 };

        var output = _preprocessor.PreprocessWithDownSample(input, false, 3, 3, Seed);
        output.RawData.Length.ShouldBe(3);
    }

    [Fact]
    public void TestDownSampleMaxGreaterThanLength()
    {
        var input = new double[] { 1, 2, 3, 4, 5 };

        var output = _preprocessor.PreprocessWithDownSample(input, false, maxArraySize: 10, seed: Seed);
        input.ShouldBe(output.RawData);
    }
}