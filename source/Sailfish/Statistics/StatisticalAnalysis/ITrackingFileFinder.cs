using Accord.Collections;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, string beforeTarget, OrderedDictionary<string, string> tags);
}