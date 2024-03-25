using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Decompositions;

internal sealed class JaggedLuDecomposition : ICloneable
{
    private int cols;
    private double[][] lowerTriangularFactor;
    private double[][] lu;
    private bool? nonsingular;
    private int pivotSign;
    private int rows;
    private double[][] upperTriangularFactor;

    public JaggedLuDecomposition(double[][] value, bool transpose = false, bool inPlace = false)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Matrix cannot be null.");
        lu = !transpose ? inPlace ? value : value.MemberwiseClone<double>() : value.Transpose(inPlace);
        rows = lu.Length;
        cols = lu[0].Length;
        pivotSign = 1;
        PivotPermutationVector = new int[rows];
        for (var index = 0; index < rows; ++index)
            PivotPermutationVector[index] = index;
        var numArray = new double[rows];
        for (var val2 = 0; val2 < cols; ++val2)
        {
            for (var index = 0; index < rows; ++index)
                numArray[index] = lu[index][val2];
            for (var val1 = 0; val1 < rows; ++val1)
            {
                var num1 = 0.0;
                var num2 = Math.Min(val1, val2);
                for (var index = 0; index < num2; ++index)
                    num1 += lu[val1][index] * numArray[index];
                lu[val1][val2] = numArray[val1] -= num1;
            }

            var index1 = val2;
            for (var index2 = val2 + 1; index2 < rows; ++index2)
                if (Math.Abs(numArray[index2]) > Math.Abs(numArray[index1]))
                    index1 = index2;

            if (index1 != val2)
            {
                for (var index3 = 0; index3 < cols; ++index3)
                {
                    (lu[index1][index3], lu[val2][index3]) = (lu[val2][index3], lu[index1][index3]);
                }

                (PivotPermutationVector[index1], PivotPermutationVector[val2]) = (PivotPermutationVector[val2], PivotPermutationVector[index1]);
                pivotSign = -pivotSign;
            }

            if (val2 >= rows || lu[val2][val2] == 0.0) continue;

            for (var index4 = val2 + 1; index4 < rows; ++index4)
                lu[index4][val2] /= lu[val2][val2];
        }
    }

    private JaggedLuDecomposition()
    {
    }

    private bool NonSingular
    {
        get
        {
            if (nonsingular.HasValue) return nonsingular.Value;
            if (rows != cols)
                throw new InvalidOperationException("Matrix must be square.");
            var flag = true;
            for (var index = 0; (index < rows) & flag; ++index)
                if (lu[index][index] == 0.0)
                    flag = false;

            nonsingular = flag;

            return nonsingular.Value;
        }
    }

    private double[][] LowerTriangularFactor
    {
        get
        {
            if (lowerTriangularFactor != null) return lowerTriangularFactor;
            var numArray = new double[rows][];
            for (var index1 = 0; index1 < rows; ++index1)
            {
                numArray[index1] = new double[rows];
                for (var index2 = 0; index2 < rows; ++index2)
                    numArray[index1][index2] = index1 <= index2 ? index1 != index2 ? 0.0 : 1.0 : lu[index1][index2];
            }

            lowerTriangularFactor = numArray;

            return lowerTriangularFactor;
        }
    }

    private double[][] UpperTriangularFactor
    {
        get
        {
            if (upperTriangularFactor != null) return upperTriangularFactor;
            var numArray = new double[rows][];
            for (var index1 = 0; index1 < rows; ++index1)
            {
                numArray[index1] = new double[cols];
                for (var index2 = 0; index2 < cols; ++index2)
                    numArray[index1][index2] = index1 > index2 ? 0.0 : lu[index1][index2];
            }

            upperTriangularFactor = numArray;

            return upperTriangularFactor;
        }
    }

    private int[] PivotPermutationVector { get; init; }

    public object Clone()
    {
        return new JaggedLuDecomposition
        {
            rows = rows,
            cols = cols,
            lu = lu.MemberwiseClone<double>(),
            pivotSign = pivotSign,
            PivotPermutationVector = PivotPermutationVector
        };
    }

    public double[][] Inverse()
    {
        if (!NonSingular)
            throw new SingularMatrixException("Matrix is singular.");
        var numArray = new double[rows][];
        for (var index1 = 0; index1 < rows; ++index1)
        {
            numArray[index1] = new double[rows];
            var index2 = PivotPermutationVector[index1];
            numArray[index1][index2] = 1.0;
        }

        for (var index3 = 0; index3 < rows; ++index3)
        for (var index4 = index3 + 1; index4 < rows; ++index4)
        for (var index5 = 0; index5 < rows; ++index5)
            numArray[index4][index5] -= numArray[index3][index5] * lu[index4][index3];

        for (var index6 = rows - 1; index6 >= 0; --index6)
        {
            for (var index7 = 0; index7 < rows; ++index7)
                numArray[index6][index7] /= lu[index6][index6];
            for (var index8 = 0; index8 < index6; ++index8)
            for (var index9 = 0; index9 < rows; ++index9)
                numArray[index8][index9] -= numArray[index6][index9] * lu[index8][index6];
        }

        return numArray;
    }

    public double[][] Reverse()
    {
        var thing = LowerTriangularFactor.ToMultidimensionalArray().Dot(UpperTriangularFactor.ToMultidimensionalArray());

        var sortedArgs = PivotPermutationVector.ArgSort();
        var res = thing.Get(sortedArgs);
        return res.ToJaggedz();
    }

    public double[][] Solve(double[][] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != rows)
            throw new DimensionMismatchException(nameof(value), "The matrix should have the same number of rows as the decomposition.");
        if (!NonSingular)
            throw new InvalidOperationException("Matrix is singular.");
        var length = value[0].Length;
        var numArray = value.Get(PivotPermutationVector, null);
        for (var index1 = 0; index1 < cols; ++index1)
        for (var index2 = index1 + 1; index2 < cols; ++index2)
        for (var index3 = 0; index3 < length; ++index3)
            numArray[index2][index3] -= numArray[index1][index3] * lu[index2][index1];

        for (var index4 = cols - 1; index4 >= 0; --index4)
        {
            for (var index5 = 0; index5 < length; ++index5)
                numArray[index4][index5] /= lu[index4][index4];
            for (var index6 = 0; index6 < index4; ++index6)
            for (var index7 = 0; index7 < length; ++index7)
                numArray[index6][index7] -= numArray[index4][index7] * lu[index6][index4];
        }

        return numArray;
    }

    private static T[][] Create<T>(int rows, int columns, params T[] values)
    {
        if (values.Length == 0)
        {
            var mat = InternalOps.Zeros<T>(rows, columns);
            return mat;
        }

        var thing = values.Reshape(rows, columns);
        return thing;
    }
}