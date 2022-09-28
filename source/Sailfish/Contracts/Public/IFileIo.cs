using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public interface IFileIo
{
    Task WriteToFile(string content, string filePath, CancellationToken cancellationToken);
    List<TData> ReadCsvFile<TMap, TData>(string filePath) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvFile<TMap, TData>(FileStream fileStream) where TMap : ClassMap where TData : class;
    List<TData> ReadCsvString<TMap, TData>(string csvContent) where TMap : ClassMap where TData : class;
}