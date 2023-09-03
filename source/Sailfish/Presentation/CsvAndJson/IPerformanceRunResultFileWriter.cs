using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.CsvAndJson;

internal interface IPerformanceRunResultFileWriter
{
    Task WriteToFileAsCsv(IEnumerable<IExecutionSummary> results, string filePath, Func<IExecutionSummary, bool> summaryFilter, CancellationToken cancellationToken);
    Task<string> ConvertToCsvStringContent(IEnumerable<IExecutionSummary> results, CancellationToken cancellationToken);
    Task WriteToFileAsJson(IEnumerable<IExecutionSummary> results, string filePath, CancellationToken cancellationToken, JsonSerializerOptions? options = null);
    Task<string> ConvertToJson(IEnumerable<IExecutionSummary> executionSummaries, CancellationToken cancellationToken, JsonSerializerOptions? options = null);
}