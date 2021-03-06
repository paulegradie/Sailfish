using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Statistics;
using Sailfish.Utils;

namespace Sailfish.Presentation.Csv;

internal class PerformanceCsvTrackingWriter : IPerformanceCsvTrackingWriter
{
    private readonly IFileIo fileIo;

    public PerformanceCsvTrackingWriter(IFileIo fileIo)
    {
        this.fileIo = fileIo;
    }

    public async Task<string> ConvertToCsvStringContent(List<ExecutionSummary> result)
    {
        var filePath = Path.GetTempFileName();
        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<TestCaseDescriptiveStatisticsMap>();

            foreach (var container in result)
            {
                var records = container
                    .CompiledResults
                    .Select(x => x.DescriptiveStatistics);

                await csv.WriteRecordsAsync(records);
            }
        }

        using var stream = new StreamReader(filePath);
        return await stream.ReadToEndAsync();
    }
}