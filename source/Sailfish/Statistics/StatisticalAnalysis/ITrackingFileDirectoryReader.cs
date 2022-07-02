using System.Collections.Generic;

namespace Sailfish.Statistics.StatisticalAnalysis;

public interface ITrackingFileDirectoryReader
{
    List<string> DefaultReadDirectory(string directory);
}