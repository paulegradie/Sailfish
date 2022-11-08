using Accord.Collections;

namespace Sailfish.Analysis;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, string beforeTarget, OrderedDictionary<string, string> tags);
}