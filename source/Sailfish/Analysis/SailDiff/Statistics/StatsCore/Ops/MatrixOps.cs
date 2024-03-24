using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Decompositions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static partial class InternalOps
{
    public static double[][] Zeros(int rows, int columns)
    {
        return Zeros<double>(rows, columns);
    }

    public static T[][] Zeros<T>(int rows, int columns)
    {
        var objArray = new T[rows][];
        for (var index = 0; index < objArray.Length; ++index)
            objArray[index] = new T[columns];
        return objArray;
    }

    public static int Rows<T>(this T[,] matrix)
    {
        return matrix.GetLength(0);
    }

    public static int Rows<T>(this T[] vector)
    {
        return vector.Length;
    }

    public static T[,] Transpose<T>(this T[,] matrix, bool inPlace)
    {
        var length1 = matrix.GetLength(0);
        var length2 = matrix.GetLength(1);
        if (inPlace)
        {
            if (length1 != length2)
                throw new ArgumentException("Only square matrices can be transposed in place.", nameof(matrix));
            for (var index1 = 0; index1 < length1; ++index1)
                for (var index2 = index1; index2 < length2; ++index2)
                {
                    (matrix[index2, index1], matrix[index1, index2]) = (matrix[index1, index2], matrix[index2, index1]);
                }

            return matrix;
        }

        var objArray = new T[length2, length1];
        for (var index3 = 0; index3 < length1; ++index3)
            for (var index4 = 0; index4 < length2; ++index4)
                objArray[index4, index3] = matrix[index3, index4];

        return objArray;
    }

    public static void CopyTo<T>(this T[,] matrix, T[,] destination, bool transpose = false)
    {
        if (matrix == destination)
        {
            if (transpose)
                matrix.Transpose(true);
        }
        else
        {
            if (transpose)
            {
                var rows = Math.Min(matrix.Rows(), destination.Columns());
                var cols = Math.Min(matrix.Columns(), destination.Rows());
                for (var i = 0; i < rows; i++)
                    for (var j = 0; j < cols; j++)
                        destination[j, i] = matrix[i, j];
            }
            else
            {
                if (matrix.Length == destination.Length)
                {
                    Array.Copy(matrix, 0, destination, 0, matrix.Length);
                }
                else
                {
                    var rows = Math.Min(matrix.Rows(), destination.Rows());
                    var cols = Math.Min(matrix.Columns(), destination.Columns());
                    for (var i = 0; i < rows; i++)
                        for (var j = 0; j < cols; j++)
                            destination[i, j] = matrix[i, j];
                }
            }
        }
    }

    public static int Columns<T>(this T[][] matrix)
    {
        if (matrix.Length == 0)
            return 0;
        return matrix[0].Length;
    }

    public static T[,] ToUpperTriangular<T>(this T[,] matrix, MatrixType from, T[,]? result = null)
    {
        result ??= CreateAs(matrix);
        matrix.CopyTo(result);
        switch (from)
        {
            case MatrixType.LowerTriangular:
                result.Transpose(true);
                goto case MatrixType.UpperTriangular;
            case MatrixType.UpperTriangular:
            case MatrixType.Diagonal:
                return result;

            default:
                throw new ArgumentException("Only LowerTriangular, UpperTriangular and Diagonal matrices are supported at this time.", "matrixType");
        }
    }

    public static double[,] Identity(int size)
    {
        return Diagonal(size, 1.0);
    }

    public static T[,] Diagonal<T>(int size, T value)
    {
        return Diagonal(size, value, new T[size, size]);
    }

    public static T[,] Diagonal<T>(int size, T value, T[,] result)
    {
        for (var index = 0; index < size; ++index)
            result[index, index] = value;
        return result;
    }

    public static T[,] Diagonal<T>(T[] values)
    {
        return Diagonal(values, new T[values.Length, values.Length]);
    }

    public static T[,] Diagonal<T>(T[] values, T[,] result)
    {
        for (var index = 0; index < values.Length; ++index)
            result[index, index] = values[index];
        return result;
    }

    public static T[] Diagonal<T>(this T[,] matrix)
    {
        var objArray = matrix != null ? new T[matrix.GetLength(0)] : throw new ArgumentNullException(nameof(matrix));
        for (var index = 0; index < objArray.Length; ++index)
            objArray[index] = matrix[index, index];
        return objArray;
    }

    public static T[,] Diagonal<T>(int rows, int cols, T[] values)
    {
        return Diagonal(rows, cols, values, new T[rows, cols]);
    }

    public static T[,] Diagonal<T>(int rows, int cols, T[] values, T[,] result)
    {
        var num = Math.Min(rows, Math.Min(cols, values.Length));
        for (var index = 0; index < num; ++index)
            result[index, index] = values[index];
        return result;
    }

    public static T[,] GetLowerTriangle<T>(this T[,] matrix, bool includeDiagonal = true)
    {
        var num = includeDiagonal ? 1 : 0;
        var lowerTriangle = CreateAs(matrix);
        for (var index1 = 0; index1 < matrix.Rows(); ++index1)
            for (var index2 = 0; index2 < index1 + num; ++index2)
                lowerTriangle[index1, index2] = matrix[index1, index2];

        return lowerTriangle;
    }

    public static T[][] Create<T>(int rows, int columns, params T[] values)
    {
        return values.Length == 0 ? Zeros<T>(rows, columns) : values.Reshape(rows, columns);
    }

    public static T[][] Reshape<T>(this T[] values, int rows, int columns)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (rows * columns != values.Length) throw new ArgumentException("The total size of the new dimensions must match the size of the array.");

        var reshapedArray = new T[rows][];
        for (var i = 0; i < rows; i++)
        {
            reshapedArray[i] = new T[columns];
            for (var j = 0; j < columns; j++) reshapedArray[i][j] = values[i * columns + j];
        }

        return reshapedArray;
    }

    public static T[] Get<T>(this T[] source, int[] indexes, bool inPlace = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(indexes);
        if (inPlace && source.Length != indexes.Length)
            throw new DimensionMismatchException("Source and indexes arrays must have the same dimension for in-place operations.");
        var objArray = new T[indexes.Length];
        for (var index1 = 0; index1 < indexes.Length; ++index1)
        {
            var index2 = indexes[index1];
            objArray[index1] = index2 < 0 ? source[source.Length + index2] : source[index2];
        }

        if (inPlace)
            for (var index = 0; index < objArray.Length; ++index)
                source[index] = objArray[index];

        return objArray;
    }

    private static double[,] Inverse(this double[,] matrix, bool inPlace)
    {
        var length1 = matrix.GetLength(0);
        var length2 = matrix.GetLength(1);
        if (length1 != length2)
            throw new ArgumentException("Matrix must be square", nameof(matrix));
        switch (length1)
        {
            case 2:
                var num1 = matrix[0, 0];
                var num2 = matrix[0, 1];
                var num3 = matrix[1, 0];
                var num4 = matrix[1, 1];
                var num5 = num1 * num4 - num2 * num3;
                if (num5 == 0.0)
                    throw new SingularMatrixException();
                var num6 = 1.0 / num5;
                var numArray1 = inPlace ? matrix : new double[2, 2];
                numArray1[0, 0] = num6 * num4;
                numArray1[0, 1] = -num6 * num2;
                numArray1[1, 0] = -num6 * num3;
                numArray1[1, 1] = num6 * num1;
                return numArray1;

            case 3:
                var num7 = matrix[0, 0];
                var num8 = matrix[0, 1];
                var num9 = matrix[0, 2];
                var num10 = matrix[1, 0];
                var num11 = matrix[1, 1];
                var num12 = matrix[1, 2];
                var num13 = matrix[2, 0];
                var num14 = matrix[2, 1];
                var num15 = matrix[2, 2];
                var num16 = num7 * (num11 * num15 - num12 * num14) - num8 * (num10 * num15 - num12 * num13) + num9 * (num10 * num14 - num11 * num13);
                if (num16 == 0.0)
                    throw new SingularMatrixException();
                var num17 = 1.0 / num16;
                var numArray2 = inPlace ? matrix : new double[3, 3];
                numArray2[0, 0] = num17 * (num11 * num15 - num12 * num14);
                numArray2[0, 1] = num17 * (num9 * num14 - num8 * num15);
                numArray2[0, 2] = num17 * (num8 * num12 - num9 * num11);
                numArray2[1, 0] = num17 * (num12 * num13 - num10 * num15);
                numArray2[1, 1] = num17 * (num7 * num15 - num9 * num13);
                numArray2[1, 2] = num17 * (num9 * num10 - num7 * num12);
                numArray2[2, 0] = num17 * (num10 * num14 - num11 * num13);
                numArray2[2, 1] = num17 * (num8 * num13 - num7 * num14);
                numArray2[2, 2] = num17 * (num7 * num11 - num8 * num10);
                return numArray2;

            default:
                return new LuDecomposition(matrix, false, inPlace).Inverse();
        }
    }

    public static double[,] Inverse(this double[,] matrix)
    {
        return matrix.Inverse(false);
    }

    public static double[][] Inverse(this double[][] matrix)
    {
        return matrix.Inverse(false);
    }

    public static double[][] Inverse(this double[][] matrix, bool inPlace)
    {
        var length1 = matrix.Length;
        var length2 = matrix[0].Length;
        if (length1 != length2)
            throw new ArgumentException("Matrix must be square", nameof(matrix));
        switch (length1)
        {
            case 2:
                var num1 = matrix[0][0];
                var num2 = matrix[0][1];
                var num3 = matrix[1][0];
                var num4 = matrix[1][1];
                var num5 = num1 * num4 - num2 * num3;
                if (num5 == 0.0)
                    throw new SingularMatrixException();
                var num6 = 1.0 / num5;
                var numArray1 = matrix;
                if (!inPlace)
                {
                    numArray1 = new double[2][];
                    for (var index = 0; index < numArray1.Length; ++index)
                        numArray1[index] = new double[2];
                }

                numArray1[0][0] = num6 * num4;
                numArray1[0][1] = -num6 * num2;
                numArray1[1][0] = -num6 * num3;
                numArray1[1][1] = num6 * num1;
                return numArray1;

            case 3:
                var num7 = matrix[0][0];
                var num8 = matrix[0][1];
                var num9 = matrix[0][2];
                var num10 = matrix[1][0];
                var num11 = matrix[1][1];
                var num12 = matrix[1][2];
                var num13 = matrix[2][0];
                var num14 = matrix[2][1];
                var num15 = matrix[2][2];
                var num16 = num7 * (num11 * num15 - num12 * num14) - num8 * (num10 * num15 - num12 * num13) + num9 * (num10 * num14 - num11 * num13);
                if (num16 == 0.0)
                    throw new SingularMatrixException();
                var num17 = 1.0 / num16;
                var numArray2 = matrix;
                if (!inPlace)
                {
                    numArray2 = new double[3][];
                    for (var index = 0; index < numArray2.Length; ++index)
                        numArray2[index] = new double[3];
                }

                numArray2[0][0] = num17 * (num11 * num15 - num12 * num14);
                numArray2[0][1] = num17 * (num9 * num14 - num8 * num15);
                numArray2[0][2] = num17 * (num8 * num12 - num9 * num11);
                numArray2[1][0] = num17 * (num12 * num13 - num10 * num15);
                numArray2[1][1] = num17 * (num7 * num15 - num9 * num13);
                numArray2[1][2] = num17 * (num9 * num10 - num7 * num12);
                numArray2[2][0] = num17 * (num10 * num14 - num11 * num13);
                numArray2[2][1] = num17 * (num8 * num13 - num7 * num14);
                numArray2[2][2] = num17 * (num7 * num11 - num8 * num10);
                return numArray2;

            default:
                return new JaggedLuDecomposition(matrix, inPlace: inPlace).Inverse();
        }
    }
}