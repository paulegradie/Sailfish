using System.Linq;

namespace Sailfish.Analysis.Scalefish;

public static class Normalizer
{
    public static double[] MinMaxNormalize(this double[] data)
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