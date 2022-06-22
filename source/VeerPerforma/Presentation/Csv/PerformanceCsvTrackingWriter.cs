using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using VeerPerforma.Statistics;
using VeerPerforma.Utils;

namespace VeerPerforma.Presentation.Csv;

public class PerformanceCsvTrackingWriter : IPerformanceCsvTrackingWriter
{
    private readonly IFileIo fileIo;

    public PerformanceCsvTrackingWriter(IFileIo fileIo)
    {
        this.fileIo = fileIo;
    }

    public async Task Present(List<CompiledResultContainer> result, string filePath, bool noTrack)
    {
        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<TestCaseStatisticMap>();

            foreach (var container in result)
            {
                if (noTrack) continue;

                var records = container
                    .CompiledResults
                    .Select(x => x.TestCaseStatistics);

                csv.WriteRecords(records);
            }
        }

        await Task.CompletedTask;
    }
}