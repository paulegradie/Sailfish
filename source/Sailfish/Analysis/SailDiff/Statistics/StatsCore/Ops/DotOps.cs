using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static class DotOps
{
    public static double[,] DotWithTransposed(this double[,] a, double[,] b)
    {
        var matrixA = Matrix<double>.Build.DenseOfArray(a);
        var matrixB = Matrix<double>.Build.DenseOfArray(b);

        var result = matrixA * matrixB.Transpose();
        return result.ToArray();
    }

    public static double[] Dot(this double[,] a, IEnumerable<double> columnVector)
    {
        return a.Dot(columnVector, new double[a.Rows()]);
    }
}