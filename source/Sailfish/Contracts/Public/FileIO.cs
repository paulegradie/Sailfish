using System.Collections;
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
}