using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public interface IFileIo
{
    Task WriteToFile(string content, string filePath, CancellationToken cancellationToken);
    List<TData> ReadCsvFileAsSync<TMap, TData>(string filePath) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvFileAsSync<TMap, TData>(FileStream fileStream) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvStringAsSync<TMap, TData>(string csvContent) where TMap : ClassMap where TData : class;

    Task<List<TData>> ReadCsvFile<TMap, TData>(string filePath) where TMap : ClassMap where TData : class;
}