using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public class FileIo : IFileIo
{
    public Task WriteDataAsJsonToFile<TData>(TData data, string outputPath, CancellationToken cancellationToken) where TData : class
    {
        // 
    }

    public async Task WriteDataAsCsvToFile<TMap, TData>(IEnumerable<TData> data, string outputPath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class
    {
        await using var writer = new StreamWriter(outputPath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();
        await csv.WriteRecordsAsync(data, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteStringToFile(string content, string filePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

        await File.WriteAllTextAsync(filePath, content, cancellationToken).ConfigureAwait(false);
        File.SetAttributes(filePath, FileAttributes.ReadOnly);
    }

    public async Task<string> WriteAsCsvToString<TMap, TData>(IEnumerable<TData> csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class
    {
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();
        await csv.WriteRecordsAsync(csvRows, cancellationToken);
        return writer.ToString();
    }

    public string WriteAsJsonToString<TData>(IEnumerable<TData> csvRows) where TData : class
    {
        var serialized = JsonSerializer.Serialize(csvRows, new JsonSerializerOptions());
        return serialized;
    }

    public TData? ReadFromJson<TData>(string content, JsonSerializerOptions options) where TData : class
    {
        var data = JsonSerializer.Deserialize<TData>(content, options);
        return data;
    }

    public async Task<string> WriteToString<TMap, TData>(IEnumerable<TData> csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class
    {
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();
        await csv.WriteRecordsAsync(csvRows, cancellationToken);
        return writer.ToString();
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