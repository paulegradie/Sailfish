using System.Linq;

namespace Sailfish.ComplexityEstimation;

public static class Normalizer
{
    public static double[] Normalize(this double[] data)
    {
        var min = data.Min();
        var max = data.Max();
        var range = max - min;

        var normalizedData = new double[data.Length];

        for (var i = 0; i < data.Length; i++)
        {
            normalizedData[i] = (data[i] - min) / range;
        }

        return normalizedData;
    }
}