using CsvHelper;
using Sailfish.Contracts.Private.CsvMaps;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Presentation.CsvAndJson;

internal interface IPerformanceRunResultFileWriter
{
    Task WriteToFileAsCsv(IEnumerable<IClassExecutionSummary> results, string filePath, Func<IClassExecutionSummary, bool> summaryFilter, CancellationToken cancellationToken);
}

internal class PerformanceRunResultFileWriter : IPerformanceRunResultFileWriter
{
    public async Task WriteToFileAsCsv(IEnumerable<IClassExecutionSummary> results, string filePath, Func<IClassExecutionSummary, bool> summaryFilter,
        CancellationToken cancellationToken)
    {
        var summaryToDescriptive = ExtractPerformanceRunResults(results.Where(summaryFilter)).ToList();
        if (summaryToDescriptive.Count == 0) return;

        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<WriteAsCsvMap>();
        await csv.WriteRecordsAsync(summaryToDescriptive, cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<PerformanceRunResult> ExtractPerformanceRunResults(IEnumerable<IClassExecutionSummary> results)
    {
        return results.SelectMany(container => container
            .CompiledTestCaseResults
            .Select(x => x.PerformanceRunResult)
            .Where(x => x is not null)
            .Cast<PerformanceRunResult>());
    }
}