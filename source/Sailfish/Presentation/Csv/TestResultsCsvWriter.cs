using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;

namespace Sailfish.Presentation.Csv;

public class TestResultsCsvWriter : ITestResultsCsvWriter
{
    // TODO: Use FileIo instead
    public async Task WriteToFile(IEnumerable<TestCaseResults> csvRows, string outputPath, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(outputPath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TestResultCsvMap>();
        await csv.WriteRecordsAsync(csvRows, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> WriteToString<TMap, TData>(IEnumerable<TData> csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class
    {
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();
        await csv.WriteRecordsAsync(csvRows, cancellationToken);
        return writer.ToString();
    }
}