using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;

namespace Sailfish.Analysis;

internal interface ITrackingFileParser
{
    Task<bool> TryParse(string fileKey, List<DescriptiveStatisticsResult> data, CancellationToken cancellationToken);
    Task<bool> TryParse(IEnumerable<string> fileKeys, List<DescriptiveStatisticsResult> data, CancellationToken cancellationToken);
}