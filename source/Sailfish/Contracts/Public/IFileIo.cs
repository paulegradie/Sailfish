using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public interface IFileIo
{
    Task WriteDataAsJsonToFile<TData>(TData data, string outputPath, CancellationToken cancellationToken) where TData : class;
    Task WriteDataAsCsvToFile<TMap, TData>(IEnumerable<TData> data, string outputPath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class;
    Task WriteStringToFile(string content, string filePath, CancellationToken cancellationToken);
    Task<string> WriteAsCsvToString<TMap, TData>(IEnumerable<TData> csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class;
    string WriteAsJsonToString<TData>(IEnumerable<TData> csvRows) where TData : class;
    TData? ReadFromJson<TData>(string content, JsonSerializerOptions options) where TData : class;
    Task<List<TData>> ReadCsvFile<TMap, TData>(string filePath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class;
    Task<List<TData>> ReadCsvString<TMap, TData>(string csvContent, CancellationToken cancellationToken) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvFileAsSync<TMap, TData>(string filePath) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvFileAsSync<TMap, TData>(FileStream fileStream) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvStringAsSync<TMap, TData>(string csvContent) where TMap : ClassMap where TData : class;
}