using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Sailfish.Utils;

internal class FileIo : IFileIo
{
    public async Task WriteToFile(string content, string filePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        File.SetAttributes(filePath, FileAttributes.ReadOnly);
    }

    public List<TData> ReadCsvFile<TMap, TData>(string filePath) where TMap : ClassMap where TData : class
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<TMap>();

            var records = csv.GetRecords<TData>().ToList();
            return records;
        }
    }

    public List<TData> ReadCsvFile<TMap, TData>(FileStream fileStream) where TMap : ClassMap where TData : class
    {
        using (var reader = new StreamReader(fileStream))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<TMap>();

            var records = csv.GetRecords<TData>().ToList();
            return records;
        }
    }
}