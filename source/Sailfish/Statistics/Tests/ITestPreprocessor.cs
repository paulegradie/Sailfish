namespace Sailfish.Statistics.Tests;

public interface ITestPreprocessor
{
    double[] Preprocess(double[] input, bool useInnerQuartile);
}