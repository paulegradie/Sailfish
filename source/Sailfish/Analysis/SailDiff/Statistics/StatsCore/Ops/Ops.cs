using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static partial class InternalOps
{
    public static double WeightedStandardDeviation(this double[] values, double[]? weights)
    {
        return Math.Sqrt(values.WeightedVariance(weights));
    }

    private static double WeightedVariance(this double[] values, double[]? weights)
    {
        return values.WeightedVariance(weights, values.WeightedMean(weights), true);
    }

    public static double WeightedVariance(this double[] values, double[]? weights, double mean)
    {
        return values.WeightedVariance(weights, mean, true);
    }

    private static double WeightedVariance(
        this double[] values,
        double[]? weights,
        double mean,
        bool unbiased,
        WeightType weightType = WeightType.Fraction)
    {
        if (values.Length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var sum = 0.0;
        var squareSum = 0.0;
        var weightSum = 0.0;
        for (var index = 0; index < values.Length; ++index)
        {
            var num = values[index] - mean;
            var weight = weights[index];
            sum += weight * (num * num);
            weightSum += weight;
            squareSum += weight * weight;
        }

        return Correct(unbiased, weightType, sum, weightSum, squareSum);
    }

    public static double WeightedStandardDeviation(this double[] values, int[] weights)
    {
        return Math.Sqrt(values.WeightedVariance(weights));
    }

    private static double WeightedVariance(this double[] values, int[] weights)
    {
        return values.WeightedVariance(weights, values.WeightedMean(weights), true);
    }

    private static double WeightedVariance(
        this IReadOnlyList<double> values,
        int[] weights,
        double mean,
        bool unbiased)
    {
        if (values.Count != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var num1 = 0.0;
        var num2 = 0;
        for (var index = 0; index < values.Count; ++index)
        {
            var num3 = values[index] - mean;
            var weight = weights[index];
            num1 += weight * (num3 * num3);
            num2 += weight;
        }

        return unbiased ? num1 / (num2 - 1.0) : num1 / num2;
    }

    public static T WeightedMode<T>(
        this T[] values,
        double[]? weights,
        bool inPlace = false,
        bool alreadySorted = false)
    {
        if (values.Length == 0)
            throw new ArgumentException("The values vector cannot be empty.", nameof(values));
        return (object)values[0] is IComparable ? weighted_mode_sort(values, weights, inPlace, alreadySorted) : weighted_mode_bag(values, weights);
    }

    public static T WeightedMode<T>(
        this T[] values,
        int[] weights,
        bool inPlace = false,
        bool alreadySorted = false)
    {
        if (values.Length == 0)
            throw new ArgumentException("The values vector cannot be empty.", nameof(values));
        return (object)values[0]! is IComparable ? weighted_mode_sort(values, weights, inPlace, alreadySorted) : weighted_mode_bag(values, weights);
    }

    private static T weighted_mode_bag<T>(IReadOnlyList<T> values, IReadOnlyList<double>? weights) where T : notnull
    {
        var obj = values[0];
        var num = 1.0;
        var dictionary = new Dictionary<T, double>();
        for (var index = 0; index < values.Count; ++index)
        {
            var key = values[index];
            if (!dictionary.TryGetValue(key, out var weight))
                weight = weights[index];
            else
                weight += weights[index];
            dictionary[key] = weight;
            if (!(weight > num)) continue;
            num = weight;
            obj = key;
        }

        return obj;
    }

    private static T weighted_mode_sort<T>(
        T[] values,
        IReadOnlyList<double>? weights,
        bool inPlace,
        bool alreadySorted)
    {
        if (!alreadySorted)
        {
            if (!inPlace)
                values = (T[])values.Clone();
            Array.Sort(values);
        }

        var obj1 = values[0];
        var weight = weights[0];
        var obj2 = obj1;
        var num = weight;
        for (var index = 1; index < values.Length; ++index)
        {
            if (obj1.Equals(values[index]))
            {
                weight += weights[index];
            }
            else
            {
                obj1 = values[index];
                weight = weights[index];
            }

            if (!(weight > num)) continue;
            num = weight;
            obj2 = obj1;
        }

        return obj2;
    }

    private static T weighted_mode_bag<T>(IReadOnlyList<T> values, IReadOnlyList<int> weights)
    {
        var obj = values[0];
        var num = 1;
        var dictionary = new Dictionary<T, int>();
        for (var index = 0; index < values.Count; ++index)
        {
            var key = values[index];
            if (!dictionary.TryGetValue(key, out var weight))
                weight = weights[index];
            else
                weight += weights[index];
            dictionary[key] = weight;
            if (weight <= num) continue;
            num = weight;
            obj = key;
        }

        return obj;
    }

    private static T weighted_mode_sort<T>(
        T[] values,
        IReadOnlyList<int> weights,
        bool inPlace,
        bool alreadySorted)
    {
        if (!alreadySorted)
        {
            if (!inPlace)
                values = (T[])values.Clone();
            Array.Sort(values);
        }

        var obj1 = values[0];
        var weight = weights[0];
        var obj2 = obj1;
        var num = weight;
        for (var index = 1; index < values.Length; ++index)
        {
            if (obj1.Equals(values[index]))
            {
                weight += weights[index];
            }
            else
            {
                obj1 = values[index];
                weight = weights[index];
            }

            if (weight <= num) continue;
            num = weight;
            obj2 = obj1;
        }

        return obj2;
    }

    public static T[,] MemberwiseClone<T>(this T[,] pre)
    {
        var a = pre.ToJaggedz();

        var objArray = new T[a.Length][];
        for (var index = 0; index < a.Length; ++index)
            objArray[index] = (T[])a[index].Clone();
        return objArray.ToMultidimensionalArray();
    }

    private static T[,] ToMultidimensionalArray<T>(this T[][] jaggedArray)
    {
        var rows = jaggedArray.Length;
        var cols = jaggedArray[0].Length;

        var multiArray = new T[rows, cols];

        for (var i = 0; i < rows; i++)
        for (var j = 0; j < cols; j++)
            multiArray[i, j] = jaggedArray[i][j];

        return multiArray;
    }

    private static bool IsVector(this Array array)
    {
        return array.Rank == 1 && !array.IsJagged();
    }

    private static object GetValue(this Array array, bool deep, int[] indices)
    {
        if (array.IsVector() || !deep || !array.IsJagged())
            return array.GetValue(indices);
        var array1 = array.GetValue(indices[0]) as Array;
        if (indices.Length == 1)
            return array1;
        var indices1 = indices.Get(1, 0);
        return array1.GetValue(true, indices1);
    }

    private static void SetValue(this Array array, object value, bool deep, int[] indices)
    {
        if (deep && array.IsJagged())
        {
            var array1 = array.GetValue(indices[0]) as Array;
            var numArray = indices.Get(1, 0);
            array1.SetValue(value, true, numArray);
        }
        else
        {
            array.SetValue(value, indices);
        }
    }

    private static object To(this Array array, Type outputType)
    {
        var elementType1 = array.GetType().GetElementType();
        var elementType2 = outputType.GetElementType();
        Array instance;
        if (elementType1.IsArray && !elementType2.IsArray)
        {
            instance = Array.CreateInstance(elementType2, array.GetLength());
            foreach (var index in instance.GetIndices())
            {
                var inputValue = array.GetValue(true, index);
                var obj = ConvertValue(elementType2, inputValue);
                instance.SetValue(obj, index);
            }
        }
        else if (!elementType1.IsArray && elementType2.IsArray)
        {
            instance = Array.CreateInstance(elementType2, array.GetLength(0));
            foreach (var index in array.GetIndices())
            {
                var inputValue = array.GetValue(index);
                var obj = ConvertValue(elementType2, inputValue);
                instance.SetValue(obj, true, index);
            }
        }
        else
        {
            instance = Array.CreateInstance(elementType2, array.GetLength(false));
            foreach (var index in array.GetIndices())
            {
                var inputValue = array.GetValue(index);
                var obj = ConvertValue(elementType2, inputValue);
                instance.SetValue(obj, index);
            }
        }

        return instance;
    }

    private static object ConvertValue(Type outputElementType, object inputValue)
    {
        var array = inputValue as Array;
        return !outputElementType.IsEnum
            ? array == null ? Convert.ChangeType(inputValue, outputElementType) : array.To(outputElementType)
            : Enum.ToObject(outputElementType, (int)Convert.ChangeType(inputValue, typeof(int)));
    }

    private static T[,] CreateAs<T>(T[,] matrix)
    {
        return new T[matrix.GetLength(0), matrix.GetLength(1)];
    }

    public static IEnumerable<long> EnumerableRange(long n)
    {
        for (long i = 0; i < n; ++i)
            yield return i;
    }

    private static IEnumerable<int[]> GetIndices(this Array array, bool inPlace = false)
    {
        return ((int[])array).Sequences(inPlace);
    }

    private static bool IsJagged(this Array array)
    {
        if (array.Length == 0)
            return array.Rank == 1;
        return array.Rank == 1 && array.GetValue(0) is Array;
    }

    private static int[] GetLength(this Array array, bool deep = true, bool max = false)
    {
        if (array.Rank == 0)
            return [];
        if (deep && array.IsJagged())
        {
            if (array.Length == 0)
                return [];
            int[] length1;
            if (!max)
            {
                length1 = (array.GetValue(0) as Array).GetLength(deep);
            }
            else
            {
                length1 = (array.GetValue(0) as Array).GetLength(deep);
                for (var index1 = 1; index1 < array.Length; ++index1)
                {
                    var length2 = (array.GetValue(index1) as Array).GetLength(deep);
                    for (var index2 = 0; index2 < length2.Length; ++index2)
                        if (length2[index2] > length1[index2])
                            length1[index2] = length2[index2];
                }
            }

            return array.Length.Concatenate(length1);
        }

        var length = new int[array.Rank];
        for (var dimension = 0; dimension < length.Length; ++dimension)
            length[dimension] = array.GetUpperBound(dimension) + 1;
        return length;
    }

    public static T[] Concatenate<T>(this T element, T[] vector)
    {
        var objArray = new T[vector.Length + 1];
        objArray[0] = element;
        for (var index = 0; index < vector.Length; ++index)
            objArray[index + 1] = vector[index];
        return objArray;
    }

    public static T[] Get<T>(this T[] source, int startRow, int endRow)
    {
        startRow = TheIndex(startRow, source.Length);
        endRow = TheEnd(endRow, source.Length);
        var objArray = new T[endRow - startRow];
        for (var index = startRow; index < endRow; ++index)
            objArray[index - startRow] = source[index];
        return objArray;
    }

    private static int TheEnd(int end, int length)
    {
        if (end <= 0)
            end = length + end;
        return end;
    }

    private static int TheIndex(int end, int length)
    {
        if (end < 0)
            end = length + end;
        return end;
    }

    public static T[,] Copy<T>(this T[,] a)
    {
        return (T[,])a.Clone();
    }

    public static T[] Copy<T>(this T[] a)
    {
        return (T[])a.Clone();
    }

    public static double WeightedMean(this double[] values, double[]? weights)
    {
        if (values.Length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var num1 = 0.0;
        for (var index = 0; index < values.Length; ++index)
            num1 += weights[index] * values[index];
        var num2 = 0.0;
        for (var index = 0; index < weights.Length; ++index)
            num2 += weights[index];
        return num1 / num2;
    }

    public static double WeightedMean(this double[] values, int[] weights)
    {
        if (values.Length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var num1 = values.Select((t, index) => weights[index] * t).Sum();
        var num2 = Enumerable.Sum(weights);
        return num1 / num2;
    }

    private static double Correct(
        bool unbiased,
        WeightType weightType,
        double sum,
        double weightSum,
        double squareSum)
    {
        if (!unbiased) return sum / weightSum;
        switch (weightType)
        {
            case WeightType.Repetition:
                return sum / (weightSum - squareSum / weightSum);

            case WeightType.Fraction:
                return sum / (weightSum - squareSum / weightSum);

            case WeightType.Automatic:
                return weightSum > 1.0 && 1E-08.IsInteger() ? sum / (weightSum - 1.0) : sum / (weightSum - squareSum / weightSum);

            case WeightType.None:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(weightType), weightType, null);
        }

        return sum / weightSum;
    }

    private static bool IsInteger(this double x, double threshold = 1.49322178960515E-300)
    {
        var num1 = Math.Round(x);
        var num2 = x;
        if (num1 == num2)
            return true;
        var num3 = Math.Abs(num1) * threshold;
        return Math.Abs(num1 - num2) <= num3;
    }

    public static double Mean(this double[] values)
    {
        var num = 0.0;
        for (var index = 0; index < values.Length; ++index)
            num += values[index];
        return num / values.Length;
    }

    public static double StandardDeviation(this double[] values, bool unbiased = true)
    {
        return values.StandardDeviation(values.Mean(), unbiased);
    }

    public static double[,] Dot(this double[,] a, double[,] b)
    {
        return a.Dot(b, new double[a.Rows(), b.Columns()]);
    }

    public static int Columns<T>(this T[,] matrix)
    {
        return matrix.GetLength(1);
    }

    public static double[,] Dot(this double[,] a, double[,] b, double[,] result)
    {
        var matrixA = Matrix<double>.Build.DenseOfArray(a);
        var matrixB = Matrix<double>.Build.DenseOfArray(b);

        if (matrixA.ColumnCount != matrixB.RowCount ||
            result.GetLength(0) != matrixA.RowCount ||
            result.GetLength(1) != matrixB.ColumnCount)
            throw new ArgumentException("Matrix dimensions are not aligned for dot product.");

        var resultMatrix = matrixA.Multiply(matrixB);

        for (var i = 0; i < result.GetLength(0); i++)
        for (var j = 0; j < result.GetLength(1); j++)
            result[i, j] = resultMatrix[i, j];

        return result;
    }

    public static double[] Dot(
        this double[,] matrix,
        IEnumerable<double> columnVector,
        double[] result)
    {
        var matrixA = Matrix<double>.Build.DenseOfArray(matrix);

        var vectorB = Vector<double>.Build.DenseOfEnumerable(columnVector);

        if (matrixA.ColumnCount != vectorB.Count ||
            result.Length != matrixA.RowCount)
            throw new ArgumentException("Dimensions of matrix and vector are not compatible for dot product.");

        var resultVector = matrixA.Multiply(vectorB);

        for (var i = 0; i < result.Length; i++) result[i] = resultVector[i];

        return result;
    }

    public static int[] Find<T>(this T[] data, Func<T, bool> func)
    {
        var intList = new List<int>();
        for (var index = 0; index < data.Length; ++index)
            if (func(data[index]))
                intList.Add(index);

        return [.. intList];
    }

    private static T[] GetColumn<T>(this T[,] m, int index, T[]? result = null)
    {
        result ??= new T[m.Rows()];
        index = TheIndex(index, m.Columns());
        for (var index1 = 0; index1 < result.Length; ++index1)
            result[index1] = m[index1, index];
        return result;
    }

    private static T[] GetRow<T>(this T[,] m, int index, T[]? result = null)
    {
        result ??= new T[m.GetLength(1)];
        index = TheIndex(index, m.Rows());
        for (var index1 = 0; index1 < result.Length; ++index1)
            result[index1] = m[index, index1];
        return result;
    }

    private static T[][] ToJaggedz<T>(this T[,] matrix, bool transpose = false)
    {
        T[][] jagged;
        if (transpose)
        {
            var length = matrix.GetLength(1);
            jagged = new T[length][];
            for (var index = 0; index < length; ++index)
                jagged[index] = matrix.GetColumn(index);
        }
        else
        {
            var length = matrix.GetLength(0);
            jagged = new T[length][];
            for (var index = 0; index < length; ++index)
                jagged[index] = matrix.GetRow(index);
        }

        return jagged;
    }

    public static T[] Concatenate<T>(this T[] a, params T[] b)
    {
        var objArray = new T[a.Length + b.Length];
        for (var index = 0; index < a.Length; ++index)
            objArray[index] = a[index];
        for (var index = 0; index < b.Length; ++index)
            objArray[index + a.Length] = b[index];
        return objArray;
    }
}