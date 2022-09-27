using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Csv;

internal class PerformanceCsvTrackingWriter : IPerformanceCsvTrackingWriter
{
    public async Task<string> ConvertToCsvStringContent(List<ExecutionSummary> result)
    {
        var filePath = Path.GetTempFileName();
        await using (var writer = new StreamWriter(filePath))
        await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<TestCaseDescriptiveStatisticsMap>();

            foreach (var records in result.Select(container => container
                         .CompiledResults
                         .Select(x => x.DescriptiveStatistics)
                         .Where(x => x is not null)))
            {
                await csv.WriteRecordsAsync(records);
            }
        }

        using var stream = new StreamReader(filePath);
        return await stream.ReadToEndAsync();
    }
}