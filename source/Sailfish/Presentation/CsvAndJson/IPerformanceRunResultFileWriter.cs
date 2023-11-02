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
}