using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Execution;

namespace Sailfish.Presentation.CsvAndJson;

internal class PerformanceRunResultFileWriter : IPerformanceRunResultFileWriter
{
    private readonly IFileIo fileIo = new FileIo();

    public async Task WriteToFileAsJson(IEnumerable<IClassExecutionSummary> results, string filePath, CancellationToken cancellationToken, JsonSerializerOptions? options = null)
    {
        var summaryToDescriptive = ExtractPerformanceRunResults(results);
        await fileIo.WriteDataAsJsonToFile(summaryToDescriptive, filePath, cancellationToken, options);
    }

    public async Task<string> ConvertToJson(IEnumerable<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken, JsonSerializerOptions? options = null)
    {
        await Task.CompletedTask;
        var summaryToDescriptive = ExtractPerformanceRunResults(executionSummaries);
        return fileIo.WriteAsJsonToString(summaryToDescriptive, options);
    }

    public async Task<string> ConvertToCsvStringContent(IEnumerable<IClassExecutionSummary> results, CancellationToken cancellationToken)
    {
        var summaryToDescriptive = ExtractPerformanceRunResults(results);
        return await fileIo
            .WriteAsCsvToString<WriteAsCsvMap, IEnumerable<PerformanceRunResult>>(
                summaryToDescriptive,
                cancellationToken);
    }

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