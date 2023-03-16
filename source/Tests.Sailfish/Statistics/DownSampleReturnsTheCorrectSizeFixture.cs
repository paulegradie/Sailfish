using System;
using System.Threading.Tasks;
using Sailfish.Statistics.Tests;
using Shouldly;
using Xunit;

namespace Test.Statistics;

public class DownSampleFixture : IAsyncLifetime
{
    private double[] data = null!;
    private readonly TestPreprocessor preprocessor = new();

    public Task InitializeAsync()
    {
        data = new[]
        {
            100.0, 100, 100, 100, 99, 100, 100, 100, 100, 100, 100, 102, 200, 43, 12, 12, 200, 43, 12, 12
        };
        return Task.CompletedTask;
    }

    [Fact]
    public void DownSampleReturnsCorrectSize()
    {
        preprocessor.PreprocessWithDownSample(data, false, true, 1).Length.ShouldBe(3);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeB()
    {
        preprocessor.PreprocessWithDownSample(data, false, true, 3).Length.ShouldBe(3);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeC()
    {
        preprocessor.PreprocessWithDownSample(data, false, true, 10).Length.ShouldBe(10);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeD()
    {
        preprocessor.PreprocessWithDownSample(data, false, true, 50).Length.ShouldBe(data.Length);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeE()
    {
        preprocessor.PreprocessWithDownSample(data, false, true, 1, 5).Length.ShouldBe(5);
    }

    [Fact]
    public void DownSampleReturnsCorrectSizeF()
    {
        preprocessor.PreprocessWithDownSample(data, false, true, 10).Length.ShouldBe(10);
    }


    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}