using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;

namespace Sailfish.Presentation.Csv;

public class TestResultsCsvWriter : ITestResultsCsvWriter
{
    public async Task WriteToFile(IEnumerable<TestCaseResults> csvRows, string outputPath, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(outputPath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TestResultCsvMap>();
        await csv.WriteRecordsAsync(csvRows, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> WriteToString(IEnumerable<TestCaseResults> csvRows, CancellationToken cancellationToken)
    {
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(csvRows, cancellationToken);
        return writer.ToString();
    }
}