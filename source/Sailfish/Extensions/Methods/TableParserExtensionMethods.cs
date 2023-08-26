using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sailfish.Exceptions;

namespace Sailfish.Extensions.Methods;

internal static class TableParserExtensionMethods
{
    public static string ToStringTable<T>(
        this IEnumerable<T> values,
        params Expression<Func<T, object>>[] valueSelectors)
    {
        var headers = valueSelectors.Select(func => GetProperty(func).Name).ToArray();
        var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
        return ToStringTableInner(
            values.ToArray(),
            string.Empty,
            headers.ToArray(),
            Enumerable.Range(0, headers.Length).Select(_ => string.Empty).ToArray(),
            selectors);
    }

    public static string ToStringTable<T>(
        this IEnumerable<T> values,
        IEnumerable<string> columnSuffixes,
        params Expression<Func<T, object>>[] valueSelectors)
    {
        var headers = valueSelectors.Select(func => GetProperty(func).Name).ToArray();
        var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
        return ToStringTableInner(values.ToArray(), string.Empty, headers.ToArray(), columnSuffixes.ToArray(), selectors);
    }

    public static string ToStringTable<T>(
        this IEnumerable<T> values,
        IEnumerable<string> columnSuffixes,
        IEnumerable<string> columnHeaders,
        params Expression<Func<T, object>>[] valueSelectors)
    {
        var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
        return ToStringTableInner(values.ToArray(), string.Empty, columnHeaders.ToArray(), columnSuffixes.ToArray(), selectors);
    }

    public static string ToStringTable<T>(
        this IEnumerable<T> values,
        string title,
        IEnumerable<string> columnSuffixes,
        IEnumerable<string> columnHeaders,
        params Expression<Func<T, object>>[] valueSelectors)
    {
        var selectors = valueSelectors.Select(exp => exp.Compile()).ToArray();
        return ToStringTableInner(values.ToArray(), title, columnHeaders.ToArray(), columnSuffixes.ToArray(), selectors);
    }

    private static string ToStringTableInner<T>(
        this IReadOnlyList<T> values,
        string title,
        IReadOnlyList<string> columnHeaders,
        IReadOnlyList<string> columnSuffixes,
        params Func<T, object>[] valueSelectors)
    {
        Debug.Assert(columnHeaders.Count == valueSelectors.Length);

        var arrValues = new string[values.Count + 1, valueSelectors.Length];

        if (columnSuffixes.Count > 0 && columnSuffixes.Count != columnHeaders.Count)
        {
            throw new Exception("Header suffix array length must match num columns");
        }

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

                if (columnSuffixes.Count == 0)
                {
                    arrValues[rowIndex, colIndex] = (value != null ? value.ToString() : "null")!;
                }
                else
                {
                    arrValues[rowIndex, colIndex] = value != null ? $"{value.ToString()} {columnSuffixes[colIndex]}" : "null";
                }
            }
        }

        var maxColumnsWidth = GetMaxColumnsWidth(arrValues);
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(title))
        {
            sb.AppendLine(title + "\n");
        }

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

            for (var colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                // Print cell
                var cell = "---";
                cell = cell.PadRight(maxColumnsWidth[colIndex]);
                sb.Append(" | ");
                sb.Append(cell);
            }

            sb.Append(" |");
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

    private static PropertyInfo GetProperty<T>(Expression<Func<T, object>> selector)
    {
        return selector.Body switch
        {
            UnaryExpression { Operand: MemberExpression operand } => (operand.Member as PropertyInfo) ??
                                                                     throw new SailfishException(
                                                                         $"Property selector derived from UnaryExpression must be of type {typeof(PropertyInfo)}"),
            MemberExpression memberExpression => (memberExpression.Member as PropertyInfo) ?? throw new SailfishException($"Property selector derived from "),
            _ => throw new SailfishException("")
        };
    }
}