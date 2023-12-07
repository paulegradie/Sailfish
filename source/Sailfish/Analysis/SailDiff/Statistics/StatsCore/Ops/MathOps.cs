namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static partial class InternalOps
{
    public static double[] Add(this double[] a, double[] b, double[] result)
    {
        for (var index = 0; index < a.Length; ++index)
            result[index] = a[index] + b[index];
        return result;
    }
}