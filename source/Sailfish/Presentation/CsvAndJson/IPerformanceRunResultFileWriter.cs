using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.CsvAndJson;

internal interface IPerformanceRunResultFileWriter
{
    Task WriteToFileAsCsv(IEnumerable<IClassExecutionSummary> results, string filePath, Func<IClassExecutionSummary, bool> summaryFilter, CancellationToken cancellationToken);
    Task<string> ConvertToCsvStringContent(IEnumerable<IClassExecutionSummary> results, CancellationToken cancellationToken);
    Task WriteToFileAsJson(IEnumerable<IClassExecutionSummary> results, string filePath, CancellationToken cancellationToken, JsonSerializerOptions? options = null);
    Task<string> ConvertToJson(IEnumerable<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken, JsonSerializerOptions? options = null);
}