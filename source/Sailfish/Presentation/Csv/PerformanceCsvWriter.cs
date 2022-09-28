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

internal class PerformanceCsvWriter : IPerformanceCsvWriter
{
    private readonly IFileIo fileIo;

    public PerformanceCsvWriter(IFileIo fileIo)
    {
        this.fileIo = fileIo;
    }

    public async Task Present(List<ExecutionSummary> result, string filePath)
    {
        await using (var writer = new StreamWriter(filePath))
        await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<TestCaseDescriptiveStatisticsMap>();

            foreach (var container in result)
            {
                if (container.Settings.AsCsv)
                {
                    var records = container.CompiledResults.Select(x => x.DescriptiveStatisticsResult);
                    await csv.WriteRecordsAsync(records);
                }
            }
        }

        await Task.CompletedTask;
    }
}