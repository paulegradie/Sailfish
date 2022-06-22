using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace VeerPerforma.Utils;

public interface IFileIo
{
    Task WriteToFile(string content, string filePath, CancellationToken cancellationToken);
    List<TData> ReadCsvFile<TMap, TData>(string filePath) where TMap : ClassMap where TData : class;
}