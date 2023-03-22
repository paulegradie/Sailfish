using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using Sailfish.Contracts.Public;

namespace Sailfish.Presentation.Csv;

public interface ITestResultsCsvWriter
{
    Task WriteToFile(IEnumerable<TestCaseResults> csvRows, string outputPath, CancellationToken cancellationToken);
    Task<string> WriteToString<TMap, TData>(IEnumerable<TData> csvRows, CancellationToken cancellationToken) where TMap : ClassMap where TData : class;
}