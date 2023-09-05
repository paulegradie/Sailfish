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
        AssertColumnHeadersMatchColumnsOrThrow(columnHeaders, columnSuffixes);
        return WriteTable(title, values, columnHeaders, columnSuffixes, valueSelectors);
    }

    private static void AssertColumnHeadersMatchColumnsOrThrow(IReadOnlyCollection<string> columnHeaders, IReadOnlyCollection<string> columnSuffixes)
    {
        if (columnSuffixes.Count > 0 && columnSuffixes.Count != columnHeaders.Count)
        {
            throw new Exception("Header suffix array length must match num columns");
        }
    }

    private static string WriteTable<T>(
        string title,
        IReadOnlyList<T> values,
        IReadOnlyList<string> columnHeaders,
        IReadOnlyList<string> columnSuffixes,
        IReadOnlyList<Func<T, object>> valueSelectors)
    {
        var internalMatrix = PopulateMatrix(values, columnHeaders, columnSuffixes, valueSelectors, valueSelectors.Count, values.Count);
        var maxColumnsWidth = ComputeColumnWidths<T>(internalMatrix.GetLength(1), internalMatrix.GetLength(0), internalMatrix);

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(title))
        {
            sb.AppendLine(title + "\n");
        }

        for (var rowIndex = 0; rowIndex < internalMatrix.GetLength(0); rowIndex++)
        {
            PrintRow(internalMatrix, maxColumnsWidth, rowIndex, sb);
        }

        return sb.ToString();
    }

    private static int[] ComputeColumnWidths<T>(int numCols, int numRows, string[,] internalMatrix)
    {
        var maxColumnsWidth = new int[numCols];
        foreach (var colIndex in Enumerable.Range(0, numCols))
        {
            var things = Enumerable.Range(0, numRows).Select(rowIndex => internalMatrix[rowIndex, colIndex].ToString().Length);
            maxColumnsWidth[colIndex] = things.Max();
        }

        return maxColumnsWidth;
    }

    private static string[,] PopulateMatrix<T>(IReadOnlyList<T> values, IReadOnlyList<string> columnHeaders, IReadOnlyList<string> columnSuffixes,
        IReadOnlyList<Func<T, object>> valueSelectors, int numCols, int numRows)
    {
        var headerHasAnyValues = columnHeaders.Any(x => !string.IsNullOrEmpty(x));
        var internalMatrix = new string[(headerHasAnyValues ? numRows + 2 : numRows), numCols];
        if (headerHasAnyValues)
        {
            // Fill Headers
            for (var colIndex = 0; colIndex < numCols; colIndex++)
            {
                internalMatrix[0, colIndex] = columnHeaders[colIndex];
            }

            for (var colIndex = 0; colIndex < numCols; colIndex++)
            {
                internalMatrix[1, colIndex] = "---";
            }
        }

        var adjustedNumRows = (headerHasAnyValues ? numRows + 2 : numRows);
        for (var rowIndex = headerHasAnyValues ? 2 : 0; rowIndex < adjustedNumRows; rowIndex++)
        {
            for (var colIndex = 0; colIndex < numCols; colIndex++)
            {
                var value = valueSelectors[colIndex].Invoke(values[headerHasAnyValues ? rowIndex - 2 : rowIndex]);
                var cell = columnSuffixes.Count == 0
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    ? (value is null ? "null" : value.ToString() ?? string.Empty).Trim()
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    : (value is null ? "null" : $"{value.ToString()?.Trim() ?? string.Empty} {columnSuffixes[colIndex]}").Trim();

                internalMatrix[rowIndex, colIndex] = cell.Trim();
            }
        }

        return internalMatrix;
    }

    private static void PrintRow(string[,] internalMatrix, IReadOnlyList<int> maxColumnsWidth, int rowIndex, StringBuilder sb)
    {
        sb.Append("| ");
        var numCols = internalMatrix.GetLength(1);
        for (var colIndex = 0; colIndex < numCols; colIndex++)
        {
            var cell = internalMatrix[rowIndex, colIndex].Trim();
            cell = colIndex == 0 || cell.Equals("---") ? cell.PadRight(maxColumnsWidth[colIndex]) : cell.PadLeft(maxColumnsWidth[colIndex]);
            sb.Append(cell);
            if (colIndex <= numCols)
            {
                sb.Append(" | ");
            }
        }

        sb.AppendLine();
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