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

internal class PerformanceResultPresenter : IPerformanceResultPresenter
{
    private readonly IFileIo fileIo = new FileIo();

    public async Task WriteToFileAsJson(IEnumerable<IExecutionSummary> results, string filePath, CancellationToken cancellationToken, JsonSerializerOptions? options = null)
    {
        var summaryToDescriptive = ExtractDescriptiveStatistics(results);
        await fileIo.WriteDataAsJsonToFile(summaryToDescriptive, filePath, cancellationToken, options);
    }

    public async Task<string> ConvertToJson(IEnumerable<IExecutionSummary> results, CancellationToken cancellationToken, JsonSerializerOptions? options = null)
    {
        await Task.CompletedTask;
        var summaryToDescriptive = ExtractDescriptiveStatistics(results);
        return fileIo.WriteAsJsonToString(summaryToDescriptive, options);
    }

    public async Task<string> ConvertToCsvStringContent(IEnumerable<IExecutionSummary> results, CancellationToken cancellationToken)
    {
        var summaryToDescriptive = ExtractDescriptiveStatistics(results);
        return await fileIo
            .WriteAsCsvToString<DescriptiveStatisticsResultCsvMap, IEnumerable<DescriptiveStatisticsResult>>(
                summaryToDescriptive,
                cancellationToken);
    }

    public async Task WriteToFileAsCsv(IEnumerable<IExecutionSummary> results, string filePath, Func<IExecutionSummary, bool> summaryFilter, CancellationToken cancellationToken)
    {
        var summaryToDescriptive = ExtractDescriptiveStatistics(results.Where(summaryFilter)).ToList();
        if (summaryToDescriptive.Count == 0) return;
        await fileIo
            .WriteDataAsCsvToFile<DescriptiveStatisticsResultCsvMap, IEnumerable<DescriptiveStatisticsResult>>(
                summaryToDescriptive,
                filePath,
                cancellationToken);
    }

    private static IEnumerable<DescriptiveStatisticsResult> ExtractDescriptiveStatistics(IEnumerable<IExecutionSummary> results)
    {
        return results.SelectMany(container => container
            .CompiledTestCaseResults
            .Select(x => x.DescriptiveStatisticsResult)
            .Where(x => x is not null)
            .Cast<DescriptiveStatisticsResult>());
    }
}