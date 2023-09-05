using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Extensions.Types;

namespace Sailfish.Analysis;

internal interface ITrackingFileParser
{
    Task<bool> TryParse(string trackingFile, TrackingFileDataList data, CancellationToken cancellationToken);
    Task<bool> TryParse(IEnumerable<string> trackingFiles, TrackingFileDataList data, CancellationToken cancellationToken);
}