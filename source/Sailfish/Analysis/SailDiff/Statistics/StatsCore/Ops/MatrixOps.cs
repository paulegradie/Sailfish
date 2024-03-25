using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static partial class InternalOps
{
    private static double[][] Zeros(int rows, int columns)
    {
        return Zeros<double>(rows, columns);
    }

    private static T[][] Zeros<T>(int rows, int columns)
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

    private static void CopyTo<T>(this T[,] matrix, T[,] destination, bool transpose = false)
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

    private static int Columns<T>(this T[][] matrix)
    {
        return matrix.Length == 0 ? 0 : matrix[0].Length;
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

    private static T[,] Diagonal<T>(int size, T value)
    {
        return Diagonal(size, value, new T[size, size]);
    }

    private static T[,] Diagonal<T>(int size, T value, T[,] result)
    {
        for (var index = 0; index < size; ++index)
            result[index, index] = value;
        return result;
    }

    public static T[,] Diagonal<T>(T[] values)
    {
        return Diagonal(values, new T[values.Length, values.Length]);
    }

    private static T[,] Diagonal<T>(IReadOnlyList<T> values, T[,] result)
    {
        for (var index = 0; index < values.Count; ++index)
            result[index, index] = values[index];
        return result;
    }

    public static T[,] Diagonal<T>(int rows, int cols, T[] values)
    {
        return Diagonal(rows, cols, values, new T[rows, cols]);
    }

    private static T[,] Diagonal<T>(int rows, int cols, T[] values, T[,] result)
    {
        var num = Math.Min(rows, Math.Min(cols, values.Length));
        for (var index = 0; index < num; ++index)
            result[index, index] = values[index];
        return result;
    }

    private static T[][] Create<T>(int rows, int columns, params T[] values)
    {
        return values.Length == 0 ? Zeros<T>(rows, columns) : values.Reshape(rows, columns);
    }

    private static T[][] Reshape<T>(this T[] values, int rows, int columns)
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
}