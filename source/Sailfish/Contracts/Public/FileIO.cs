using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public class FileIo : IFileIo
{
    private readonly JsonSerializerOptions DefaultSerializerOptions = new();

    public async Task WriteDataAsJsonToFile<TData>(TData data, string outputPath, CancellationToken cancellationToken, JsonSerializerOptions? options = null)
        where TData : class, IEnumerable
    {
        var serialized = WriteAsJsonToString(data, options);
        await WriteStringToFile(serialized, outputPath, cancellationToken);
    }

    public async Task WriteDataAsCsvToFile<TMap, TData>(TData data, string outputPath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class, IEnumerable
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

    public async Task<string> WriteAsCsvToString<TMap, TData>(TData csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class, IEnumerable
    {
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<TMap>();
        await csv.WriteRecordsAsync(csvRows, cancellationToken);
        return writer.ToString();
    }

    public string WriteAsJsonToString<TData>(TData data, JsonSerializerOptions? options = null) where TData : class, IEnumerable
    {
        var opts = options ?? DefaultSerializerOptions;
        opts.Converters.Add(new JsonNanConverter());
        return JsonSerializer.Serialize(data, options ?? DefaultSerializerOptions);
    }

    public TData? ReadFromJson<TData>(string content, JsonSerializerOptions? options = null) where TData : class
    {
        var data = JsonSerializer.Deserialize<TData>(content, options ?? DefaultSerializerOptions);
        return data;
    }

    public async Task<string> WriteToString<TMap, TData>(TData csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class, IEnumerable
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

internal class JsonNanConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TryGetDouble(out var value))
        {
            return value;
        }

        var stringValue = reader.GetString();
        if (stringValue != null && stringValue.Equals("NaN", StringComparison.InvariantCultureIgnoreCase))
        {
            return double.NaN;
        }

        throw new JsonException($"Unable to parse value (using custom parser): {stringValue}");
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN");
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}