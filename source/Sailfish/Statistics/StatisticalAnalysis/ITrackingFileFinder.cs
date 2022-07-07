using Accord.Collections;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, OrderedDictionary<string, string> tags);
}