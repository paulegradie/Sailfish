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
    public async Task WriteToFile(IEnumerable<NamedTTestResult> csvRows, string outputPath, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(outputPath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<NamedTTestResultMap>();
        await csv.WriteRecordsAsync(csvRows, cancellationToken).ConfigureAwait(false);
    }
}

public interface ITTestResultCsvWriter
{
    Task WriteToFile(IEnumerable<NamedTTestResult> csvRows, string outputPath, CancellationToken cancellationToken);
}