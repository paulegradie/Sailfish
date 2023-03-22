using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public class FileIo : IFileIo
{
    public async Task WriteToFile(string content, string filePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

        await File.WriteAllTextAsync(filePath, content, cancellationToken).ConfigureAwait(false);
        File.SetAttributes(filePath, FileAttributes.ReadOnly);
    }

    public async Task<List<TData>> ReadCsvFile<TMap, TData>(string filePath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();

        var records = await csv.GetRecordsAsync<TData>(cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
        return records;
    }

    public async Task<List<TData>> ReadCsvString<TMap, TData>(string csvContent, CancellationToken cancellationToken) where TMap : ClassMap where TData : class
    {
        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();

        var records = await csv.GetRecordsAsync<TData>(cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
        return records;
    }

    public List<TData> ReadCsvFileAsSync<TMap, TData>(string filePath) where TMap : ClassMap where TData : class
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();

        var records = csv.GetRecords<TData>().ToList();
        return records;
    }

    public List<TData> ReadCsvFileAsSync<TMap, TData>(FileStream fileStream) where TMap : ClassMap where TData : class
    {
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();

        var records = csv.GetRecords<TData>().ToList();
        return records;
    }

    public List<TData> ReadCsvStringAsSync<TMap, TData>(string csvContent) where TMap : ClassMap where TData : class
    {
        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();

        var records = csv.GetRecords<TData>().ToList();
        return records;
    }
}