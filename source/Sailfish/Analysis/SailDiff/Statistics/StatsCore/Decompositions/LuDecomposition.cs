using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Decompositions;

internal sealed class LuDecomposition : ICloneable, ISolverMatrixDecomposition<double>
{
    private readonly LU<double> decomposition;
    private readonly Matrix<double> matrix;

    public LuDecomposition(double[,] value) : this(value, false, false)
    {
    }

    public LuDecomposition(double[,] value, bool transpose) : this(value, transpose, false)
    {
    }

    public LuDecomposition(double[,] value, bool transpose, bool inPlace)
    {
        var matrixValue = Matrix<double>.Build.DenseOfArray(value);
        if (transpose) matrixValue = matrixValue.Transpose();
        matrix = inPlace ? matrixValue : matrixValue.Clone();
        decomposition = matrixValue.LU();
    }

    public object Clone()
    {
        return new LuDecomposition(matrix.ToArray(), false, false);
    }

    public double[,] Inverse()
    {
        return decomposition.Inverse().ToArray();
    }

    public double[,] Solve(double[,] value)
    {
        var rightSide = Matrix<double>.Build.DenseOfArray(value);
        var solution = decomposition.Solve(rightSide);
        return solution.ToArray();
    }

    public double[] Solve(double[] value)
    {
        var rightSide = Vector<double>.Build.DenseOfArray(value);
        var solution = decomposition.Solve(rightSide);
        return [.. solution];
    }

    public double[,] Reverse()
    {
        return matrix.ToArray();
    }

    public double[,] GetInformationMatrix()
    {
        var inverseCovariance = matrix.TransposeThisAndMultiply(matrix).Inverse();
        return inverseCovariance.ToArray();
    }
}