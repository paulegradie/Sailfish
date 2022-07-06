using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;

namespace Sailfish.Presentation.Csv;

public class TTestResultCsvWriter : ITTestResultCsvWriter
{
    public async Task WriteToFile(List<NamedTTestResult> csvRows, string outputPath, CancellationToken cancellationToken)
    {
        using (var writer = new StreamWriter(outputPath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<NamedTTestResultMap>();
            csv.WriteRecords(csvRows);
        }

        await Task.Yield();
    }
}

public interface ITTestResultCsvWriter
{
    Task WriteToFile(List<NamedTTestResult> csvRows, string outputPath, CancellationToken cancellationToken);
}