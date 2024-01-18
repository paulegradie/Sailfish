using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
using System;
using System.Threading.Tasks;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Decompositions;

[Serializable]
internal sealed class CholeskyDecomposition : ICloneable, ISolverMatrixDecomposition<double>
{
    private bool destroyed;

    private double[,] diagonalMatrix;
    private double[,] L;
    private double[,] leftTriangularFactor;
    private double? lndeterminant;
    private int n;
    private bool robust;

    public CholeskyDecomposition(double[,] value, bool robust = false, bool inPlace = false, MatrixType matrixType = MatrixType.UpperTriangular)
    {
        if (value.Rows() != value.Columns())
            throw new DimensionMismatchException(nameof(value), "Matrix is not square.");
        if (!inPlace)
            value = value.Copy();
        n = value.Rows();
        L = value.ToUpperTriangular(matrixType, value);
        this.robust = robust;
        if (robust)
            LDLt();
        else
            LLt();
    }

    private CholeskyDecomposition()
    {
    }

    public bool IsPositiveDefinite { get; private set; }

    public bool IsUndefined { get; private set; }

    public double[,] LeftTriangularFactor
    {
        get
        {
            if (leftTriangularFactor == null)
            {
                if (destroyed)
                    throw new InvalidOperationException("The decomposition has been destroyed.");
                if (IsUndefined)
                    throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
                leftTriangularFactor = L.GetLowerTriangle();
            }

            return leftTriangularFactor;
        }
    }

    public double[,] DiagonalMatrix
    {
        get
        {
            if (diagonalMatrix == null)
            {
                if (destroyed)
                    throw new InvalidOperationException("The decomposition has been destroyed.");
                diagonalMatrix = Ops.InternalOps.Diagonal(Diagonal);
            }

            return diagonalMatrix;
        }
    }

    public double[] Diagonal { get; private set; }

    public double LogDeterminant
    {
        get
        {
            if (!lndeterminant.HasValue)
            {
                if (destroyed)
                    throw new InvalidOperationException("The decomposition has been destroyed.");
                if (IsUndefined)
                    throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
                var num1 = 0.0;
                var num2 = 0.0;
                for (var index = 0; index < n; ++index)
                    num1 += Math.Log(L[index, index]);
                if (Diagonal != null)
                    for (var index = 0; index < Diagonal.Length; ++index)
                        num2 += Math.Log(Diagonal[index]);

                lndeterminant = num1 + num1 + num2;
            }

            return lndeterminant.Value;
        }
    }

    public object Clone()
    {
        return new CholeskyDecomposition
        {
            L = L.MemberwiseClone<double>(),
            Diagonal = (double[])Diagonal.Clone(),
            destroyed = destroyed,
            n = n,
            IsUndefined = IsUndefined,
            robust = robust,
            IsPositiveDefinite = IsPositiveDefinite
        };
    }

    public double[,] Solve(double[,] value)
    {
        return Solve(value, false);
    }

    public double[] Solve(double[] value)
    {
        return Solve(value, false);
    }

    public double[,] Inverse()
    {
        return Solve(Ops.InternalOps.Identity(n));
    }

    public double[,] Reverse()
    {
        if (destroyed)
            throw new InvalidOperationException("The decomposition has been destroyed.");
        if (IsUndefined)
            throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
        return robust
            ? LeftTriangularFactor.Dot(DiagonalMatrix).DotWithTransposed(LeftTriangularFactor)
            : LeftTriangularFactor.DotWithTransposed(LeftTriangularFactor);
    }

    public double[,] GetInformationMatrix()
    {
        var numArray = Reverse();
        var res = numArray.TransposeAndDotAlt(numArray);
        return res.Inverse();
    }

    private void LLt()
    {
        Diagonal = Vector.Ones<double>(n);
        IsPositiveDefinite = true;
        for (var index1 = 0; index1 < n; ++index1)
        {
            var num1 = 0.0;
            for (var index2 = 0; index2 < index1; ++index2)
            {
                var num2 = L[index2, index1];
                for (var index3 = 0; index3 < index2; ++index3)
                    num2 -= L[index1, index3] * L[index2, index3];
                var num3 = num2 / L[index2, index2];
                L[index1, index2] = num3;
                num1 += num3 * num3;
            }

            var d = L[index1, index1] - num1;
            IsPositiveDefinite &= d > 1E-14 * Math.Abs(L[index1, index1]);
            L[index1, index1] = Math.Sqrt(d);
        }
    }

    private void LDLt()
    {
        Diagonal = new double[n];
        IsPositiveDefinite = true;
        var v = new double[n];
        for (var i = 0; i < n; i++)
        {
            for (var index = 0; index < i; ++index)
                v[index] = L[i, index] * Diagonal[index];
            var d = 0.0;
            for (var index = 0; index < i; ++index)
                d += L[i, index] * v[index];
            d = Diagonal[i] = v[i] = L[i, i] - d;
            IsPositiveDefinite &= v[i] > 1E-14 * Math.Abs(L[i, i]);
            if (v[i] == 0.0)
            {
                IsUndefined = true;
                return;
            }

            Parallel.For(i + 1, n, k =>
            {
                var num = 0.0;
                for (var index = 0; index < i; ++index)
                    num += L[k, index] * v[index];
                L[k, i] = (L[i, k] - num) / d;
            });
        }

        for (var index = 0; index < n; ++index)
            L[index, index] = 1.0;
    }

    public double[,] Solve(double[,] value, bool inPlace)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Rows() != n)
            throw new ArgumentException("Argument matrix should have the same number of rows as the decomposed matrix.", nameof(value));
        if (!robust && !IsPositiveDefinite)
            throw new NonPositiveDefiniteMatrixException("Decomposed matrix is not positive definite.");
        if (IsUndefined)
            throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
        if (destroyed)
            throw new InvalidOperationException("The decomposition has been destroyed.");
        var matrix = inPlace ? value : value.MemberwiseClone<double>();
        var num = matrix.Columns();
        for (var index1 = 0; index1 < n; ++index1)
            for (var index2 = 0; index2 < num; ++index2)
            {
                for (var index3 = 0; index3 < index1; ++index3)
                    matrix[index1, index2] -= matrix[index3, index2] * L[index1, index3];
                matrix[index1, index2] /= L[index1, index1];
            }

        if (robust)
            for (var index4 = 0; index4 < Diagonal.Length; ++index4)
                for (var index5 = 0; index5 < num; ++index5)
                    matrix[index4, index5] /= Diagonal[index4];

        for (var index6 = n - 1; index6 >= 0; --index6)
            for (var index7 = 0; index7 < num; ++index7)
            {
                for (var index8 = index6 + 1; index8 < n; ++index8)
                    matrix[index6, index7] -= matrix[index8, index7] * L[index8, index6];
                matrix[index6, index7] /= L[index6, index6];
            }

        return matrix;
    }

    public double[] Solve(double[] value, bool inPlace)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != n)
            throw new ArgumentException("Argument vector should have the same length as rows in the decomposed matrix.", nameof(value));
        if (!robust && !IsPositiveDefinite)
            throw new NonPositiveDefiniteMatrixException("Decomposed matrix is not positive definite.");
        if (IsUndefined)
            throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
        if (destroyed)
            throw new InvalidOperationException("The decomposition has been destroyed.");
        var numArray = inPlace ? value : value.Copy();
        for (var index1 = 0; index1 < n; ++index1)
        {
            for (var index2 = 0; index2 < index1; ++index2)
                numArray[index1] -= numArray[index2] * L[index1, index2];
            numArray[index1] /= L[index1, index1];
        }

        if (robust)
            for (var index = 0; index < Diagonal.Length; ++index)
                numArray[index] /= Diagonal[index];

        for (var index3 = n - 1; index3 >= 0; --index3)
        {
            for (var index4 = index3 + 1; index4 < n; ++index4)
                numArray[index3] -= numArray[index4] * L[index4, index3];
            numArray[index3] /= L[index3, index3];
        }

        return numArray;
    }
}