﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Sailfish.ExtensionMethods;

internal static class TableParserExtensionMethods
{
    public static string ToStringTable<T>(this IEnumerable<T> values, List<string> headerSuffixes, List<Expression<Func<T, object>>> valueSelectors)
    {
        return ToStringTable(values, headerSuffixes, valueSelectors.ToArray());
    }

    public static string ToStringTable<T>(this IEnumerable<T> values, List<string> headerSuffixes, params Expression<Func<T, object>>[] valueSelectors)
    {
        var headers = valueSelectors.Select(func => GetProperty<T>(func)!.Name).ToArray();
        var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
        return ToStringTable(values, headers.ToArray(), headerSuffixes, selectors);
    }

    public static string ToStringTable<T>(this IEnumerable<T> values, string[] columnHeaders, List<string> headerSuffixes, params Func<T, object>[] valueSelectors)
    {
        return ToStringTable(values.ToArray(), columnHeaders, headerSuffixes, valueSelectors);
    }

    public static string ToStringTable<T>(this T[] values, string[] columnHeaders, List<string> headerSuffixes, params Func<T, object>[] valueSelectors)
    {
        Debug.Assert(columnHeaders.Length == valueSelectors.Length);

        var arrValues = new string[values.Length + 1, valueSelectors.Length];

        if (headerSuffixes.Count > 0 && headerSuffixes.Count != columnHeaders.Length) throw new Exception("Header suffix array length must match num columns");

        // Fill headers
        for (var colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
        {
            arrValues[0, colIndex] = columnHeaders[colIndex];
        }

        // Fill table rows
        for (var rowIndex = 1; rowIndex < arrValues.GetLength(0); rowIndex++)
        {
            for (var colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                var value = valueSelectors[colIndex].Invoke(values[rowIndex - 1]);

                if (headerSuffixes.Count == 0)
                {
                    arrValues[rowIndex, colIndex] = (value != null ? value.ToString() : "null")!;
                }
                else
                {
                    arrValues[rowIndex, colIndex] = value != null ? $"{value.ToString()} {headerSuffixes[colIndex]}" : "null";
                }
            }
        }

        return ToStringTable(arrValues);
    }

    public static string ToStringTable(this string[,] arrValues)
    {
        var maxColumnsWidth = GetMaxColumnsWidth(arrValues);
        var headerSplitter = new string('-', maxColumnsWidth.Sum(i => i + 3) - 1);

        var sb = new StringBuilder();
        for (var rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
        {
            for (var colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                // Print cell
                var cell = arrValues[rowIndex, colIndex];
                cell = cell.PadRight(maxColumnsWidth[colIndex]);
                sb.Append(" | ");
                sb.Append(cell);
            }

            // Print end of line
            sb.Append(" | ");
            sb.AppendLine();

            // Print splitter
            if (rowIndex != 0) continue;
            sb.AppendFormat(" |{0}| ", headerSplitter);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static int[] GetMaxColumnsWidth(string[,] arrValues)
    {
        var maxColumnsWidth = new int[arrValues.GetLength(1)];
        for (var colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
        {
            for (var rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                var newLength = arrValues[rowIndex, colIndex].Length;
                var oldLength = maxColumnsWidth[colIndex];

                if (newLength > oldLength)
                {
                    maxColumnsWidth[colIndex] = newLength;
                }
            }
        }

        return maxColumnsWidth;
    }


    // I'm not sure this is the best approach. Seems like we could search from custom attributes instead.
    private static PropertyInfo? GetProperty<T>(Expression<Func<T, object>> selector)
    {
        if (selector.Body is UnaryExpression)
        {
            if ((selector.Body as UnaryExpression)!.Operand is MemberExpression)
            {
                return (((selector.Body as UnaryExpression)!.Operand as MemberExpression)!.Member as PropertyInfo)!;
            }
        }

        if (selector.Body is MemberExpression memberExpression)
        {
            return (memberExpression?.Member as PropertyInfo)!;
        }

        return null;
    }
}