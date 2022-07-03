using System.Collections.Generic;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal interface ITrackingFileDirectoryReader
{
    List<string> DefaultReadDirectory(string directory);
}