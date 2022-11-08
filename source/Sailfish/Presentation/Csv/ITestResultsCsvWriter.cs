using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;

namespace Sailfish.Presentation.Csv;

public interface ITestResultsCsvWriter
{
    Task WriteToFile(IEnumerable<TestCaseResults> csvRows, string outputPath, CancellationToken cancellationToken);
}