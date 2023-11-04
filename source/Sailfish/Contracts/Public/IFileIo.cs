using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace Sailfish.Contracts.Public;

public interface IFileIo
{
    Task WriteDataAsCsvToFile<TMap, TData>(TData data, string outputPath, CancellationToken cancellationToken) where TMap : ClassMap where TData : class, IEnumerable;
    Task WriteStringToFile(string content, string filePath, CancellationToken cancellationToken);
}