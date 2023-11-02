using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Private.CsvMaps;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.Presentation.CsvAndJson;

internal class PerformanceRunResultFileWriter : IPerformanceRunResultFileWriter
{
    private readonly IFileIo fileIo = new FileIo();

    public async Task WriteToFileAsCsv(IEnumerable<IClassExecutionSummary> results, string filePath, Func<IClassExecutionSummary, bool> summaryFilter, CancellationToken cancellationToken)
    {
        var summaryToDescriptive = ExtractPerformanceRunResults(results.Where(summaryFilter)).ToList();
        if (summaryToDescriptive.Count == 0) return;
        await fileIo.WriteDataAsCsvToFile<WriteAsCsvMap, IEnumerable<PerformanceRunResult>>(summaryToDescriptive, filePath, cancellationToken);
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