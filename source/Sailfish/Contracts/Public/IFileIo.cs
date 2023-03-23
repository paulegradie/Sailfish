using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public interface IFileIo
{
    Task WriteDataAsJsonToFile<TData>(TData data, string outputPath, CancellationToken cancellationToken, JsonSerializerOptions? options = null) where TData : class, IEnumerable;
    Task WriteDataAsCsvToFile<TMap, TData>(TData data, string outputPath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class, IEnumerable;
    Task WriteStringToFile(string content, string filePath, CancellationToken cancellationToken);
    Task<string> WriteAsCsvToString<TMap, TData>(TData csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class, IEnumerable;
    string WriteAsJsonToString<TData>(TData csvRows, JsonSerializerOptions? options = null) where TData : class, IEnumerable;
    TData? ReadFromJson<TData>(string content, JsonSerializerOptions? options = null) where TData : class;
    Task<List<TData>> ReadCsvFile<TMap, TData>(string filePath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class;
    Task<List<TData>> ReadCsvString<TMap, TData>(string csvContent, CancellationToken cancellationToken) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvFileAsSync<TMap, TData>(string filePath) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvFileAsSync<TMap, TData>(FileStream fileStream) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvStringAsSync<TMap, TData>(string csvContent) where TMap : ClassMap where TData : class;
}