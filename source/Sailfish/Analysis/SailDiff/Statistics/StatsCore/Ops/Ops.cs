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

    public static double WeightedVariance(this double[] values, double[]? weights)
    {
        return values.WeightedVariance(weights, values.WeightedMean(weights), true);
    }

    public static double WeightedVariance(this double[] values, double[]? weights, double mean)
    {
        return values.WeightedVariance(weights, mean, true);
    }

    public static double WeightedVariance(
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

    public static double WeightedVariance(this double[] values, int[] weights)
    {
        return values.WeightedVariance(weights, values.WeightedMean(weights), true);
    }

    public static double WeightedVariance(
        this double[] values,
        int[] weights,
        double mean,
        bool unbiased)
    {
        if (values.Length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var num1 = 0.0;
        var num2 = 0;
        for (var index = 0; index < values.Length; ++index)
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
        return (object)values[0] is IComparable ? weighted_mode_sort(values, weights, inPlace, alreadySorted) : weighted_mode_bag(values, weights);
    }

    private static T weighted_mode_bag<T>(T[] values, double[]? weights)
    {
        var obj = values[0];
        var num = 1.0;
        var dictionary = new Dictionary<T, double>();
        for (var index = 0; index < values.Length; ++index)
        {
            var key = values[index];
            double weight;
            if (!dictionary.TryGetValue(key, out weight))
                weight = weights[index];
            else
                weight += weights[index];
            dictionary[key] = weight;
            if (weight > num)
            {
                num = weight;
                obj = key;
            }
        }

        return obj;
    }

    private static T weighted_mode_sort<T>(
        T[] values,
        double[]? weights,
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

            if (weight > num)
            {
                num = weight;
                obj2 = obj1;
            }
        }

        return obj2;
    }

    private static T weighted_mode_bag<T>(T[] values, int[] weights)
    {
        var obj = values[0];
        var num = 1;
        var dictionary = new Dictionary<T, int>();
        for (var index = 0; index < values.Length; ++index)
        {
            var key = values[index];
            int weight;
            if (!dictionary.TryGetValue(key, out weight))
                weight = weights[index];
            else
                weight += weights[index];
            dictionary[key] = weight;
            if (weight > num)
            {
                num = weight;
                obj = key;
            }
        }

        return obj;
    }

    private static T weighted_mode_sort<T>(
        T[] values,
        int[] weights,
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

            if (weight > num)
            {
                num = weight;
                obj2 = obj1;
            }
        }

        return obj2;
    }

    public static int[] ArgSort<T>(this T[] values) where T : IComparable<T>
    {
        values.Copy().Sort(out var order);
        return order;
    }

    public static T[][] Get<T>(
        this T[][] source,
        int[] rowIndexes,
        int[] columnIndexes,
        bool reuseMemory = false,
        T[][] result = null)
    {
        return source.GetInner(result, rowIndexes, columnIndexes, reuseMemory);
    }

    public static T[][] MemberwiseClone<T>(this T[][] a)
    {
        var objArray = new T[a.Length][];
        for (var index = 0; index < a.Length; ++index)
            objArray[index] = (T[])a[index].Clone();
        return objArray;
    }

    public static T[,] MemberwiseClone<T>(this T[,] pre)
    {
        var a = pre.ToJaggedz();

        var objArray = new T[a.Length][];
        for (var index = 0; index < a.Length; ++index)
            objArray[index] = (T[])a[index].Clone();
        return objArray.ToMultidimensionalArray();
    }

    public static T[,] ToMultidimensionalArray<T>(this T[][] jaggedArray)
    {
        var rows = jaggedArray.Length;
        var cols = jaggedArray[0].Length;

        var multiArray = new T[rows, cols];

        for (var i = 0; i < rows; i++)
            for (var j = 0; j < cols; j++)
                multiArray[i, j] = jaggedArray[i][j];

        return multiArray;
    }

    private static T[,] GetInner<T>(
        this T[,]? source,
        T[,]? destination,
        int[]? rowIndexes,
        int[]? columnIndexes)
    {
        var num = source?.GetLength(0) ?? throw new ArgumentNullException(nameof(source));
        var length1 = source.GetLength(1);
        var length2 = num;
        var length3 = length1;
        if (rowIndexes == null && columnIndexes == null)
            return source;
        if (rowIndexes != null)
        {
            length2 = rowIndexes.Length;
            for (var index = 0; index < rowIndexes.Length; ++index)
                if (rowIndexes[index] < 0 || rowIndexes[index] >= num)
                    throw new ArgumentException("Argument out of range.");
        }

        if (columnIndexes != null)
        {
            length3 = columnIndexes.Length;
            for (var index = 0; index < columnIndexes.Length; ++index)
                if (columnIndexes[index] < 0 || columnIndexes[index] >= length1)
                    throw new ArgumentException("Argument out of range.");
        }

        if (destination != null)
        {
            if (destination.GetLength(0) < length2 || destination.GetLength(1) < length3)
                throw new DimensionMismatchException(nameof(destination), "The destination matrix must be big enough to accommodate the results.");
        }
        else
        {
            destination = new T[length2, length3];
        }

        if (columnIndexes == null)
            for (var index1 = 0; index1 < rowIndexes.Length; ++index1)
                for (var index2 = 0; index2 < length1; ++index2)
                    destination[index1, index2] = source[rowIndexes[index1], index2];
        else if (rowIndexes == null)
            for (var index3 = 0; index3 < num; ++index3)
                for (var index4 = 0; index4 < columnIndexes.Length; ++index4)
                    destination[index3, index4] = source[index3, columnIndexes[index4]];
        else
            for (var index5 = 0; index5 < rowIndexes.Length; ++index5)
                for (var index6 = 0; index6 < columnIndexes.Length; ++index6)
                    destination[index5, index6] = source[rowIndexes[index5], columnIndexes[index6]];

        return destination;
    }

    private static T[][] GetInner<T>(
        this T[][] source,
        T[][]? destination,
        int[]? rowIndexes,
        int[]? columnIndexes,
        bool reuseMemory)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length == 0)
            return [];
        var length1 = source.Length;
        var length2 = source[0].Length;
        var length3 = length1;
        var length4 = length2;
        if (rowIndexes == null && columnIndexes == null)
            return source;
        if (rowIndexes != null)
        {
            length3 = rowIndexes.Length;
            for (var index = 0; index < rowIndexes.Length; ++index)
                if (rowIndexes[index] < 0 || rowIndexes[index] >= length1)
                    throw new ArgumentException("Argument out of range.");
        }

        if (columnIndexes != null)
        {
            length4 = columnIndexes.Length;
            for (var index = 0; index < columnIndexes.Length; ++index)
                if (columnIndexes[index] < 0 || columnIndexes[index] >= length2)
                    throw new ArgumentException("Argument out of range.");
        }

        if (destination != null)
        {
            if (destination.Length < length3)
                throw new DimensionMismatchException(nameof(destination), "The destination matrix must be big enough to accommodate the results.");
        }
        else
        {
            destination = new T[length3][];
            if (columnIndexes != null && !reuseMemory)
                for (var index = 0; index < destination.Length; ++index)
                    destination[index] = new T[length4];
        }

        if (columnIndexes == null)
        {
            if (reuseMemory)
                for (var index = 0; index < rowIndexes.Length; ++index)
                    destination[index] = source[rowIndexes[index]];
            else
                for (var index = 0; index < rowIndexes.Length; ++index)
                    destination[index] = (T[])source[rowIndexes[index]].Clone();
        }
        else if (rowIndexes == null)
        {
            for (var index1 = 0; index1 < source.Length; ++index1)
                for (var index2 = 0; index2 < columnIndexes.Length; ++index2)
                    destination[index1][index2] = source[index1][columnIndexes[index2]];
        }
        else
        {
            for (var index3 = 0; index3 < rowIndexes.Length; ++index3)
                for (var index4 = 0; index4 < columnIndexes.Length; ++index4)
                    destination[index3][index4] = source[rowIndexes[index3]][columnIndexes[index4]];
        }

        return destination;
    }

    public static double[,] WeightedScatter(
        this double[][] matrix,
        double[]? weights,
        double[] means,
        double factor,
        int dimension)
    {
        var length1 = matrix.Length;
        if (length1 == 0)
            return new double[0, 0];
        var length2 = matrix[0].Length;
        double[,] numArray;
        if (dimension == 0)
        {
            if (means.Length != length2)
                throw new DimensionMismatchException(nameof(means), "Length of the mean vector should equal the number of columns.");
            if (length1 != weights.Length)
                throw new DimensionMismatchException(nameof(weights), "The number of rows and weights must match.");
            numArray = new double[length2, length2];
            for (var index1 = 0; index1 < length2; ++index1)
                for (var index2 = index1; index2 < length2; ++index2)
                {
                    var num = 0.0;
                    for (var index3 = 0; index3 < length1; ++index3)
                        num += weights[index3] * (matrix[index3][index2] - means[index2]) * (matrix[index3][index1] - means[index1]);
                    numArray[index1, index2] = num * factor;
                    numArray[index2, index1] = num * factor;
                }
        }
        else
        {
            if (dimension != 1)
                throw new ArgumentException("Invalid dimension.", nameof(dimension));
            if (means.Length != length1)
                throw new DimensionMismatchException(nameof(means), "Length of the mean vector should equal the number of rows.");
            if (length2 != weights.Length)
                throw new DimensionMismatchException(nameof(weights), "The number of columns and weights must match.");
            numArray = new double[length1, length1];
            for (var index4 = 0; index4 < length1; ++index4)
                for (var index5 = index4; index5 < length1; ++index5)
                {
                    var num = 0.0;
                    for (var index6 = 0; index6 < length2; ++index6)
                        num += weights[index6] * (matrix[index5][index6] - means[index5]) * (matrix[index4][index6] - means[index4]);
                    numArray[index4, index5] = num * factor;
                    numArray[index5, index4] = num * factor;
                }
        }

        return numArray;
    }

    public static TOutput To<TOutput>(this Array array) where TOutput : class, ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable => array.To(typeof(TOutput)) as TOutput;

    public static bool IsVector(this Array array)
    {
        return array.Rank == 1 && !array.IsJagged();
    }

    public static object GetValue(this Array array, bool deep, int[] indices)
    {
        if (array.IsVector() || !deep || !array.IsJagged())
            return array.GetValue(indices);
        var array1 = array.GetValue(indices[0]) as Array;
        if (indices.Length == 1)
            return array1;
        var indices1 = indices.Get(1, 0);
        return array1.GetValue(true, indices1);
    }

    public static void SetValue(this Array array, object value, bool deep, int[] indices)
    {
        if (deep && array.IsJagged())
        {
            var array1 = array.GetValue(indices[0]) as Array;
            var numArray = indices.Get(1, 0);
            var obj = value;
            var indices1 = numArray;
            array1.SetValue(obj, true, indices1);
        }
        else
        {
            array.SetValue(value, indices);
        }
    }

    public static object To(this Array array, Type outputType)
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

    public static double[,] PooledCovariance(double[][,] covariances, double[] weights)
    {
        var numArray = CreateAs(covariances[0]);
        var num = weights.Sum();
        for (var index = 0; index < covariances.Length; ++index)
        {
            var weight = weights[index];
            if (num != 0.0)
                weight /= num;
            numArray.MultiplyAndAdd(weight, covariances[index], numArray);
        }

        return numArray;
    }

    public static T[,] CreateAs<T>(T[,] matrix)
    {
        return new T[matrix.GetLength(0), matrix.GetLength(1)];
    }

    public static double[,] MultiplyAndAdd(
        this double[,] a,
        double b,
        double[,] c,
        double[,] result)
    {
        for (var index1 = 0; index1 < result.GetLength(0); ++index1)
            for (var index2 = 0; index2 < result.GetLength(1); ++index2)
                result[index1, index2] = a[index1, index2] * b + c[index1, index2];

        return result;
    }

    public static IEnumerable<long> EnumerableRange(long n)
    {
        for (long i = 0; i < n; ++i)
            yield return i;
    }

    public static T[][] Split<T>(this T[] vector, int size)
    {
        var length = vector.Length / size;
        var objArray1 = new T[length][];
        for (var index1 = 0; index1 < length; ++index1)
        {
            var objArray2 = objArray1[index1] = new T[size];
            for (var index2 = 0; index2 < size; ++index2)
                objArray2[index2] = vector[index2 * length + index1];
        }

        return objArray1;
    }

    public static bool IsEqual(this double[] a, double[] b, double atol = 0.0, double rtol = 0.0)
    {
        if (a == b || (a == null && b == null))
            return true;
        if ((a == null) ^ (b == null))
            return false;
        var length1 = a.GetLength();
        var length2 = b.GetLength();
        if (length1.Length != length2.Length)
            return false;
        for (var index = 0; index < length1.Length; ++index)
            if (length1[index] != length2[index])
                return false;

        if (rtol > 0.0)
            for (var index = 0; index < a.Length; ++index)
            {
                var d1 = a[index];
                var d2 = b[index];
                if (d1 != d2 && (!double.IsNaN(d1) || !double.IsNaN(d2)))
                {
                    if (double.IsNaN(d1) ^ double.IsNaN(d2) || double.IsPositiveInfinity(d1) ^ double.IsPositiveInfinity(d2) ||
                        double.IsNegativeInfinity(d1) ^ double.IsNegativeInfinity(d2))
                        return false;
                    var num1 = d1;
                    var num2 = d2;
                    var num3 = Math.Abs(num1 - num2);
                    if (num1 == 0.0)
                    {
                        if (num3 <= rtol)
                            continue;
                    }
                    else if (num2 == 0.0 && num3 <= rtol)
                    {
                        continue;
                    }

                    if (num3 > Math.Abs(num1) * rtol)
                        return false;
                }
            }
        else if (atol > 0.0)
            for (var index = 0; index < a.Length; ++index)
            {
                var d3 = a[index];
                var d4 = b[index];
                if (d3 != d4 && (!double.IsNaN(d3) || !double.IsNaN(d4)) && (double.IsNaN(d3) ^ double.IsNaN(d4) || double.IsPositiveInfinity(d3) ^ double.IsPositiveInfinity(d4) ||
                                                                             double.IsNegativeInfinity(d3) ^ double.IsNegativeInfinity(d4) || Math.Abs(d3 - d4) > atol))
                    return false;
            }
        else
            for (var index = 0; index < a.Length; ++index)
            {
                var d5 = a[index];
                var d6 = b[index];
                if ((!double.IsNaN(d5) || !double.IsNaN(d6)) && (double.IsNaN(d5) ^ double.IsNaN(d6) || double.IsPositiveInfinity(d5) ^ double.IsPositiveInfinity(d6) ||
                                                                 double.IsNegativeInfinity(d5) ^ double.IsNegativeInfinity(d6) || d5 != d6))
                    return false;
            }

        return true;
    }

    public static double[] WeightedMean(this double[][] matrix, double[]? weights)
    {
        return matrix.WeightedMean(weights, 0);
    }

    public static double[] WeightedMean(this double[][] matrix, double[]? weights, int dimension = 0)
    {
        var length1 = matrix.Length;
        if (length1 == 0)
            return [];
        var length2 = matrix[0].Length;
        double[] numArray1;
        if (dimension == 0)
        {
            numArray1 = new double[length2];
            if (length1 != weights.Length)
                throw new DimensionMismatchException(nameof(weights), "The number of rows and weights must match.");
            for (var index1 = 0; index1 < length1; ++index1)
            {
                var numArray2 = matrix[index1];
                var weight = weights[index1];
                for (var index2 = 0; index2 < length2; ++index2)
                    numArray1[index2] += numArray2[index2] * weight;
            }
        }
        else
        {
            if (dimension != 1)
                throw new ArgumentException("Invalid dimension.", nameof(dimension));
            numArray1 = new double[length1];
            if (length2 != weights.Length)
                throw new DimensionMismatchException(nameof(weights), "The number of columns and weights must match.");
            for (var index3 = 0; index3 < length1; ++index3)
            {
                var numArray3 = matrix[index3];
                var weight = weights[index3];
                for (var index4 = 0; index4 < length2; ++index4)
                    numArray1[index3] += numArray3[index4] * weight;
            }
        }

        var num = weights.Sum();
        if (num != 0.0)
            for (var index = 0; index < numArray1.Length; ++index)
                numArray1[index] /= num;

        return numArray1;
    }

    public static IEnumerable<int[]> GetIndices(this Array array, bool inPlace = false)
    {
        return ((int[])array).Sequences(inPlace);
    }

    public static bool IsJagged(this Array array)
    {
        if (array.Length == 0)
            return array.Rank == 1;
        return array.Rank == 1 && array.GetValue(0) is Array;
    }

    public static int[] GetLength(this Array array, bool deep = true, bool max = false)
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

    public static int TheEnd(int end, int length)
    {
        if (end <= 0)
            end = length + end;
        return end;
    }

    public static int TheIndex(int end, int length)
    {
        if (end < 0)
            end = length + end;
        return end;
    }

    public static double[] Mean(this double[][] matrix, int dimension)
    {
        var length1 = matrix.Length;
        if (length1 == 0)
            return [];
        double[] numArray;
        if (dimension == 0)
        {
            var length2 = matrix[0].Length;
            numArray = new double[length2];
            double num = length1;
            for (var index1 = 0; index1 < length2; ++index1)
            {
                for (var index2 = 0; index2 < length1; ++index2)
                    numArray[index1] += matrix[index2][index1];
                numArray[index1] = numArray[index1] / num;
            }
        }
        else
        {
            if (dimension != 1)
                throw new ArgumentException("Invalid dimension.", nameof(dimension));
            numArray = new double[length1];
            for (var index3 = 0; index3 < length1; ++index3)
            {
                for (var index4 = 0; index4 < matrix[index3].Length; ++index4)
                    numArray[index3] += matrix[index3][index4];
                numArray[index3] = numArray[index3] / matrix[index3].Length;
            }
        }

        return numArray;
    }

    public static T[,] Get<T>(this T[,] source, int[] rowIndexes)
    {
        return source.GetInner(null, rowIndexes, null);
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

    public static void SetColumn<T>(this T[][] m, int index, T[] column)
    {
        index = TheIndex(index, m.Columns());
        for (var index1 = 0; index1 < column.Length; ++index1)
            m[index1][index] = column[index1];
    }

    public static double Correct(
        bool unbiased,
        WeightType weightType,
        double sum,
        double weightSum,
        double squareSum)
    {
        if (unbiased)
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

    public static bool IsInteger(this double x, double threshold = 1.49322178960515E-300)
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

    public static double[,] TransposeAndDot(this double[,] a, double[,] b)
    {
        var result = Create<double>(a.Columns(), b.Columns()).ToMultidimensionalArray();
        return a.TransposeAndDot(b, result);
    }

    public static double[,] TransposeAndDotAlt(this double[,] a, double[,] b)
    {
        return a.TransposeAndDot(b, Create<double>(a.Columns(), b.Columns()).ToMultidimensionalArray());
    }

    public static double[,] Dot(this double[,] a, double[,] b)
    {
        return a.Dot(b, new double[a.Rows(), b.Columns()]);
    }

    public static double[] Dot(this double[] rowVector, double[,] b)
    {
        return rowVector.Dot(b);
    }

    public static int Columns<T>(this T[,] matrix)
    {
        return matrix.GetLength(1);
    }

    public static double[,] TransposeAndDot(this double[,] a, double[,] b, double[,] result)
    {
        var matrixA = Matrix<double>.Build.DenseOfArray(a);
        var matrixB = Matrix<double>.Build.DenseOfArray(b);

        var transposedA = matrixA.Transpose();

        if (transposedA.ColumnCount != matrixB.RowCount ||
            result.GetLength(0) != transposedA.RowCount ||
            result.GetLength(1) != matrixB.ColumnCount)
            throw new ArgumentException("Matrix dimensions are not aligned for dot product.");

        var resultMatrix = transposedA.Multiply(matrixB);

        for (var i = 0; i < result.GetLength(0); i++)
            for (var j = 0; j < result.GetLength(1); j++)
                result[i, j] = resultMatrix[i, j];

        return result;
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
        double[] columnVector,
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

    public static T[,] ToMatrix<T>(this T[][] array, bool transpose = false)
    {
        var length1 = array.Length;
        if (length1 == 0)
            return new T[0, length1];
        var length2 = array[0].Length;
        T[,] matrix;
        if (transpose)
        {
            matrix = new T[length2, length1];
            for (var index1 = 0; index1 < length1; ++index1)
                for (var index2 = 0; index2 < length2; ++index2)
                    matrix[index2, index1] = array[index1][index2];
        }
        else
        {
            matrix = new T[length1, length2];
            for (var index3 = 0; index3 < length1; ++index3)
                for (var index4 = 0; index4 < length2; ++index4)
                    matrix[index3, index4] = array[index3][index4];
        }

        return matrix;
    }

    public static int[] Find<T>(this T[] data, Func<T, bool> func)
    {
        var intList = new List<int>();
        for (var index = 0; index < data.Length; ++index)
            if (func(data[index]))
                intList.Add(index);

        return [.. intList];
    }

    public static T[] GetColumn<T>(this T[,] m, int index, T[]? result = null)
    {
        result ??= new T[m.Rows()];
        index = TheIndex(index, m.Columns());
        for (var index1 = 0; index1 < result.Length; ++index1)
            result[index1] = m[index1, index];
        return result;
    }

    public static T[] GetRow<T>(this T[,] m, int index, T[]? result = null)
    {
        result ??= new T[m.GetLength(1)];
        index = TheIndex(index, m.Rows());
        for (var index1 = 0; index1 < result.Length; ++index1)
            result[index1] = m[index, index1];
        return result;
    }

    public static T[][] ToJaggedz<T>(this T[,] matrix, bool transpose = false)
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

    public static T[][] Transpose<T>(this T[][] matrix)
    {
        return matrix.Transpose(false);
    }

    public static bool IsRectangular<T>(T[][] matrix)
    {
        var length = matrix[0].Length;
        for (var index = 1; index < matrix.Length; ++index)
            if (matrix[index].Length != length)
                return false;

        return true;
    }

    public static T[][] Transpose<T>(this T[][] matrix, bool inPlace)
    {
        var length1 = matrix.Length;
        if (length1 == 0)
            return new T[length1][];
        var length2 = matrix[0].Length;
        if (!IsRectangular(matrix))
            throw new ArgumentException("Only rectangular matrices can be transposed.");
        if (inPlace)
        {
            if (length1 != length2)
                throw new ArgumentException("Only square matrices can be transposed in place.", nameof(matrix));
            for (var index1 = 0; index1 < length1; ++index1)
                for (var index2 = index1; index2 < length2; ++index2)
                    (matrix[index2][index1], matrix[index1][index2]) = (matrix[index1][index2], matrix[index2][index1]);

            return matrix;
        }

        var objArray = new T[length2][];
        for (var index3 = 0; index3 < length2; ++index3)
        {
            objArray[index3] = new T[length1];
            for (var index4 = 0; index4 < length1; ++index4)
                objArray[index3][index4] = matrix[index4][index3];
        }

        return objArray;
    }

    public static TResult[] Apply<TInput, TResult>(this TInput[] vector, Func<TInput, TResult> func)
    {
        return vector.Apply(func, new TResult[vector.Length]);
    }

    public static TResult[] Apply<TInput, TResult>(
        this TInput[] vector,
        Func<TInput, TResult> func,
        TResult[] result)
    {
        for (var index = 0; index < vector.Length; ++index)
            result[index] = func(vector[index]);
        return result;
    }

    public static double[][] Round(this double[][] value, double[][] result)
    {
        for (var index1 = 0; index1 < value.Length; ++index1)
            for (var index2 = 0; index2 < value[index1].Length; ++index2)
            {
                var a = value[index1][index2];
                result[index1][index2] = Math.Round(a);
            }

        return result;
    }

    public static double[][] Scatter(
        double[][] matrix,
        double[] means,
        double divisor,
        int dimension)
    {
        var length1 = matrix.Length;
        if (length1 == 0)
            return [];
        var length2 = matrix[0].Length;
        double[][] numArray;
        if (dimension == 0)
        {
            if (means.Length != length2)
                throw new ArgumentException("Length of the mean vector should equal the number of columns", nameof(means));
            numArray = Zeros(length2, length2);
            for (var index1 = 0; index1 < length2; ++index1)
                for (var index2 = index1; index2 < length2; ++index2)
                {
                    var num1 = 0.0;
                    for (var index3 = 0; index3 < length1; ++index3)
                        num1 += (matrix[index3][index2] - means[index2]) * (matrix[index3][index1] - means[index1]);
                    var num2 = num1 / divisor;
                    numArray[index1][index2] = num2;
                    numArray[index2][index1] = num2;
                }
        }
        else
        {
            if (dimension != 1)
                throw new ArgumentException("Invalid dimension.", nameof(dimension));
            if (means.Length != length1)
                throw new ArgumentException("Length of the mean vector should equal the number of rows", nameof(means));
            numArray = Zeros(length1, length1);
            for (var index4 = 0; index4 < length1; ++index4)
                for (var index5 = index4; index5 < length1; ++index5)
                {
                    var num3 = 0.0;
                    for (var index6 = 0; index6 < length2; ++index6)
                        num3 += (matrix[index5][index6] - means[index5]) * (matrix[index4][index6] - means[index4]);
                    var num4 = num3 / divisor;
                    numArray[index4][index5] = num4;
                    numArray[index5][index4] = num4;
                }
        }

        return numArray;
    }

    public static double[][] Covariance(this double[][] matrix, double[] means)
    {
        return Scatter(matrix, means, matrix.Length - 1, 0);
    }

    public static T[] Concatenate<T>(this T[][] vectors)
    {
        var length = 0;
        for (var index = 0; index < vectors.Length; ++index)
            length += vectors[index].Length;
        var objArray = new T[length];
        var num = 0;
        for (var index1 = 0; index1 < vectors.Length; ++index1)
            for (var index2 = 0; index2 < vectors[index1].Length; ++index2)
                objArray[num++] = vectors[index1][index2];

        return objArray;
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

    public static double[] Variance(this double[][] matrix, double[] means, bool unbiased = true)
    {
        var length1 = matrix.Length;
        if (length1 == 0)
            return [];
        var length2 = matrix[0].Length;
        double num1 = length1;
        var numArray = new double[length2];
        for (var index1 = 0; index1 < length2; ++index1)
        {
            var num2 = 0.0;
            var num3 = 0.0;
            for (var index2 = 0; index2 < length1; ++index2)
            {
                var num4 = matrix[index2][index1] - means[index1];
                num2 += num4;
                num3 += num4 * num4;
            }

            numArray[index1] = !unbiased ? (num3 - num2 * num2 / num1) / num1 : (num3 - num2 * num2 / num1) / (num1 - 1.0);
        }

        return numArray;
    }

    public static double[] WeightedVariance(
        this double[][] matrix,
        double[]? weights,
        double[] means)
    {
        return matrix.WeightedVariance(weights, means, true);
    }

    public static double[] WeightedVariance(
        this double[][] matrix,
        double[]? weights,
        double[] means,
        bool unbiased,
        WeightType weightType = WeightType.Fraction)
    {
        var length = matrix.Length;
        if (length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        if (length == 0)
            return [];
        var numArray = new double[matrix[0].Length];
        for (var index1 = 0; index1 < numArray.Length; ++index1)
        {
            var sum = 0.0;
            var weightSum = 0.0;
            var squareSum = 0.0;
            for (var index2 = 0; index2 < matrix.Length; ++index2)
            {
                var num = matrix[index2][index1] - means[index1];
                var weight = weights[index2];
                sum += weight * (num * num);
                weightSum += weight;
                squareSum += weight * weight;
            }

            numArray[index1] = Correct(unbiased, weightType, sum, weightSum, squareSum);
        }

        return numArray;
    }

    public static double[,] WeightedCovariance(
        this double[][] matrix,
        double[]? weights,
        double[] means)
    {
        return matrix.WeightedCovariance(weights, means, 0);
    }
}